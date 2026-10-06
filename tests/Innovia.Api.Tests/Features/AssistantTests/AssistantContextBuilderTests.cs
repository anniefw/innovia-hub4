using System.Globalization;
using Innovia.Api.Common.Database;
using Innovia.Api.Common.Database.Entities;
using Innovia.Api.Common.Time;
using Innovia.Api.Features.Assistant;

namespace Innovia.Api.Tests.Features.AssistantTests;

[Collection("Database")]
public class AssistantContextBuilderTests
{
    private readonly DatabaseFixture _fixture;

    public AssistantContextBuilderTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Hjälpmetoder ──────────────────────────────────────────────────────────

    private static async Task<(string TypeName, string ResourceName)> SeedAsync(
        AppDbContext context, ResourceStatus status)
    {
        var typeName = $"Testtyp {Guid.NewGuid():N}";
        var resourceName = $"Testresurs {Guid.NewGuid():N}";

        var resourceType = new ResourceType
        {
            Id = Guid.CreateVersion7(),
            Name = typeName,
            CreatedAt = DateTimeOffset.UtcNow,
            MaxDurationMinutes = 240,   // → "max 4 h per bokning"
            MaxAdvanceDays = 14
        };

        var resource = new Resource
        {
            Id = Guid.CreateVersion7(),
            Name = resourceName,
            Description = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            ResourceTypeId = resourceType.Id,
            Status = status
        };

        // Bara vardagar → lördag och söndag saknar regel → "kan inte bokas"
        DayOfWeek[] weekdays =
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        ];

        var rules = weekdays.Select(day => new AvailabilityRule
        {
            Id = Guid.CreateVersion7(),
            ResourceTypeId = resourceType.Id,
            DayOfWeek = day,
            OpensAt = new TimeOnly(8, 0),
            ClosesAt = new TimeOnly(16, 0),
            SlotDurationMinutes = 60
        });

        context.ResourceTypes.Add(resourceType);
        context.Resources.Add(resource);
        context.AvailabilityRules.AddRange(rules);
        await context.SaveChangesAsync();

        return (typeName, resourceName);
    }

    private static async Task<string> BuildTextAsync(AppDbContext context)
    {
        var builder = new AssistantContextBuilder(context);
        var text = await builder.BuildAsync(CancellationToken.None);
        return text.Replace("\r\n", "\n");
    }

    private static string SectionStartingWith(string text, string header)
    {
        var start = text.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Hittade inte '{header}' i texten.");

        var end = text.IndexOf("\n\n", start, StringComparison.Ordinal);
        return end < 0 ? text[start..] : text[start..end];
    }

    private static string ResourcesPart(string text)
    {
        var start = text.IndexOf("RESURSER OCH AKTUELL STATUS", StringComparison.Ordinal);
        Assert.True(start >= 0, "Hittade inte resurssektionen.");
        return text[start..];
    }

    // ── Struktur ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Contain_All_Section_Headers()
    {
        await using var context = _fixture.CreateDbContext();

        var text = await BuildTextAsync(context);

        Assert.Contains("AKTUELL TID", text);
        Assert.Contains("ALLMÄN INFORMATION", text);
        Assert.Contains("BOKNINGSBARA TIDER PER RESURSTYP", text);
        Assert.Contains("RESURSER OCH AKTUELL STATUS", text);
    }

    [Fact]
    public async Task Should_Include_Knowledge_File()
    {
        await using var context = _fixture.CreateDbContext();

        var text = await BuildTextAsync(context);

        Assert.Contains("InnoviaHub_member_wifi", text);
    }

    [Fact]
    public async Task Should_Include_Todays_Swedish_Weekday()
    {
        await using var context = _fixture.CreateDbContext();

        var text = await BuildTextAsync(context);

        var swedishNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, SwedenTimeZone.Instance);
        var todayName = swedishNow.ToString("dddd", new CultureInfo("sv-SE"));

        Assert.Contains(todayName, text);
    }

    // ── Öppettider ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Show_Opening_Hours_And_Closed_Weekend()
    {
        await using var context = _fixture.CreateDbContext();
        var (typeName, _) = await SeedAsync(context, ResourceStatus.Online);

        var text = await BuildTextAsync(context);

        var section = SectionStartingWith(text, typeName);

        Assert.Contains("max 4 h per bokning", section);
        Assert.Contains("måndag: 08:00", section);
        Assert.Contains("fredag: 08:00", section);
        Assert.Contains("lördag: kan inte bokas", section);
        Assert.Contains("söndag: kan inte bokas", section);
    }

    // ── Resursstatus ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Show_Online_Resource_As_Available()
    {
        await using var context = _fixture.CreateDbContext();
        var (typeName, resourceName) = await SeedAsync(context, ResourceStatus.Online);

        var text = await BuildTextAsync(context);

        var section = SectionStartingWith(ResourcesPart(text), $"{typeName}:");
        Assert.Contains($"Tillgängliga att boka: {resourceName}", section);
    }

    [Fact]
    public async Task Should_Show_Resource_In_Maintenance()
    {
        await using var context = _fixture.CreateDbContext();
        var (typeName, resourceName) = await SeedAsync(context, ResourceStatus.Maintenance);

        var text = await BuildTextAsync(context);

        var section = SectionStartingWith(ResourcesPart(text), $"{typeName}:");
        Assert.Contains($"Underhåll (kan tyvärr inte bokas i nuläget): {resourceName}", section);
    }

    [Fact]
    public async Task Should_Not_Show_Archived_Resource()
    {
        await using var context = _fixture.CreateDbContext();
        var (_, resourceName) = await SeedAsync(context, ResourceStatus.Archived);

        var text = await BuildTextAsync(context);

        // Arkiverade resurser ska inte nämnas någonstans
        Assert.DoesNotContain(resourceName, text);
    }
}