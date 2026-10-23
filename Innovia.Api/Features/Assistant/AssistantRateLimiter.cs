using Microsoft.Extensions.Caching.Memory;

namespace Innovia.Api.Features.Assistant;

public sealed class AssistantRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(1); 
    public const string LimitReachedMessage = "Åh nu har du ställt för många frågor! Du har max 20 frågor på en timma. Försök igen om en stund så ska jag svara då!";

    private readonly IMemoryCache _cache; 
    private readonly TimeProvider _time; 
    private readonly int _maxQuestionsPerWindow; 
    private readonly object _lock = new();

    public AssistantRateLimiter(IMemoryCache cache, TimeProvider time, int maxQuestionsPerWindow)
    {
        _cache = cache;
        _time = time;
        _maxQuestionsPerWindow = maxQuestionsPerWindow;
    }

    public bool TryConsume(string userId)
    {
        var key = $"assistant-rate:{userId}"; 
        var now = _time.GetUtcNow();

        lock(_lock) 
        {
            var window = _cache.Get<RateWindow>(key);

            if(window is null || now >= window.StartedAt + Window)
                window = new RateWindow(now, 0);

            if(window.Count >= _maxQuestionsPerWindow) return false;

            var updated = window with {Count = window.Count + 1};
            _cache.Set(key, updated, Window);

            return true;
        }

    }
    private sealed record RateWindow(DateTimeOffset StartedAt, int Count);

}