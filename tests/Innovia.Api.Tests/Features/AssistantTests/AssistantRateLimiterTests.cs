using Innovia.Api.Features.Assistant;
using Microsoft.Extensions.Caching.Memory;

namespace Innovia.Api.Tests.Features.AssistantTests;

public class AssistantRateLimiterTests
{
    // Falsk klocka: vi bestämmer själva vad klockan är, och kan spola fram.
    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static (AssistantRateLimiter Limiter, FakeTimeProvider Time) Create(int max)
    {
        var time = new FakeTimeProvider();
        var cache = new MemoryCache(new MemoryCacheOptions { Clock = null });
        return (new AssistantRateLimiter(cache, time, max), time);
    }

    [Fact]
    public void Should_Allow_Questions_Up_To_The_Limit()
    {
        var (limiter, _) = Create(max: 3);

        Assert.True(limiter.TryConsume("anna"));   // 1
        Assert.True(limiter.TryConsume("anna"));   // 2
        Assert.True(limiter.TryConsume("anna"));   // 3
    }

    [Fact]
    public void Should_Block_Question_Over_The_Limit()
    {
        var (limiter, _) = Create(max: 3);

        limiter.TryConsume("anna");
        limiter.TryConsume("anna");
        limiter.TryConsume("anna");

        Assert.False(limiter.TryConsume("anna"));   // 4 → stoppas
    }

    [Fact]
    public void Should_Count_Each_User_Separately()
    {
        var (limiter, _) = Create(max: 1);

        Assert.True(limiter.TryConsume("anna"));
        Assert.False(limiter.TryConsume("anna"));   // Anna har slut

        Assert.True(limiter.TryConsume("bertil"));  // Bertil påverkas inte
    }

    [Fact]
    public void Should_Allow_Again_When_Window_Has_Passed()
    {
        var (limiter, time) = Create(max: 1);

        Assert.True(limiter.TryConsume("anna"));
        Assert.False(limiter.TryConsume("anna"));

        time.Now = time.Now.AddHours(1);   // spola fram en timme, utan att vänta

        Assert.True(limiter.TryConsume("anna"));   // nytt fönster
    }

    [Fact]
    public void Should_Still_Block_Just_Before_Window_Ends()
    {
        // Gränsvärde: 59 min 59 s räcker INTE
        var (limiter, time) = Create(max: 1);

        limiter.TryConsume("anna");
        time.Now = time.Now.AddHours(1).AddSeconds(-1);

        Assert.False(limiter.TryConsume("anna"));
    }
}