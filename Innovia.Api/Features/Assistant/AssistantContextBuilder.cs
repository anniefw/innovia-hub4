using System.Globalization;
using System.Text;
using Innovia.Api.Common.Database;
using Innovia.Api.Common.Database.Entities;
using Innovia.Api.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Innovia.Api.Features.Assistant;

public sealed class AssistantContextBuilder
{
    private const string AssistantFolder = "Features/Assistant";
    private const string KnowledgeFileName = "knowledge.md";
    private const string SystemPromptFileName = "systemprompt.md";

    private static readonly DayOfWeek[] SwedishWeekOrder = [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private static readonly CultureInfo Swedish = new ("sv-SE");

    private readonly AppDbContext _context;

    public AssistantContextBuilder (AppDbContext context)
    {
        _context = context;
    }

    public Task<string> ReadSystemPromptAsync(CancellationToken ct) =>
        ReadAssistantFileAsync(SystemPromptFileName, ct);
     public async Task<string> BuildAsync (CancellationToken ct)
    {
        var now = BuildCurrentTime(); 
        var knowledge = await ReadAssistantFileAsync(KnowledgeFileName, ct); 
        var openingHours = await BuildOpeningHoursAsync(ct); 
        var resources = await BuildResourcesAsync(ct); 

                return $"""
            --- AKTUELL TID ---
            {now}

            --- ALLMÄN INFORMATION ---
            {knowledge}

            --- BOKNINGSBARA TIDER PER RESURSTYP ---
            {openingHours}

            --- RESURSER OCH AKTUELL STATUS ---
            {resources}
            """;                         


    }
    private static string BuildCurrentTime()
    {
        var swedishNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, SwedenTimeZone.Instance);
        
        return swedishNow.ToString("dddd d MMMM yyyy 'kl.' HH:mm", Swedish);
    }

    private async Task<string> ReadAssistantFileAsync(string fileName, CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, AssistantFolder, fileName);

        if(!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Filen '{fileName}' hittades inte på '{path}'. " +
                "Kontrollera att den finns i Features/Assistant och att .csproj kopierar den.");
        }

        var content = await File.ReadAllTextAsync(path, ct);

        return content.Trim();
    }

    private async Task<string> BuildOpeningHoursAsync(CancellationToken ct)
    {
        var resourceTypes = await _context.ResourceTypes
        .AsNoTracking()
        .OrderBy(rt => rt.Name)
        .ToListAsync(ct);

        var rules = await _context.AvailabilityRules
        .AsNoTracking()
        .ToListAsync(ct);

        var sb = new StringBuilder();

        foreach(var type in resourceTypes)
        {
            sb.AppendLine($"{type.Name} (max {FormatDuration(type.MaxDurationMinutes)} per bokning," + $" kan bokas max {type.MaxAdvanceDays} dagar i förväg):");

            
            foreach(var day in SwedishWeekOrder)
            {
                var rule = rules.FirstOrDefault(r => 
                r.ResourceTypeId == type.Id &&
                r.DayOfWeek == day);

                var dayName = Swedish.DateTimeFormat.GetDayName(day);

                if(rule is null)
                {
                    sb.AppendLine($"- {dayName}: stängt, kan inte bokas.");
                } else 
                {
                    var opens = rule.OpensAt.ToString("HH:mm");
                    var closes = rule.ClosesAt.ToString("HH:mm");
                    sb.AppendLine($"- {dayName}: {opens} - {closes}");
                    
                }
            }
            sb.AppendLine(); 
        }

        return sb.ToString().Trim(); 
            
    }

    private static string FormatDuration(int minutes) => 
        minutes % 60 == 0
        ? $"{minutes/60} h"
        : $"{minutes} min";
    private async Task<string> BuildResourcesAsync(CancellationToken ct)
    {
        var resourcetypes = await _context.ResourceTypes
            .AsNoTracking()
            .OrderBy(rt => rt.Name)
            .ToListAsync(ct);

        var resources = await _context.Resources
            .AsNoTracking()
            .Where(r => r.Status != ResourceStatus.Archived) 
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        var sb = new StringBuilder(); 

        foreach(var type in resourcetypes) 
        {
            var resourcesOfType = resources
                .Where(rot => rot.ResourceTypeId == type.Id)
                .ToList();

            if (resourcesOfType.Count == 0) continue;

            sb.AppendLine($"{type.Name}:");

            var statusGroups = resourcesOfType
            .GroupBy(r => r.Status)
            .OrderBy(g => g.Key); 

            foreach (var group in statusGroups) 

            {
                var names = string.Join(", ", group.Select(g => g.Name));

                sb.AppendLine($"-{DescribeStatus(group.Key)}: {names}");
            }
            sb.AppendLine(); 
        }

        return sb.ToString().Trim(); 
    }
    private static string DescribeStatus(ResourceStatus status) => status switch
    {
        ResourceStatus.Online => "Tillgängliga att boka",
        ResourceStatus.Offline => "Ur drift (kan tyvärr inte bokas i nuläget)",
        ResourceStatus.Maintenance => "Underhåll (kan tyvärr inte bokas i nuläget)",
        _ => status.ToString() 
    };

}
