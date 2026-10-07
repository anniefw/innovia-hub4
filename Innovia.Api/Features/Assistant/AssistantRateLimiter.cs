using Microsoft.Extensions.Caching.Memory;

namespace Innovia.Api.Features.Assistant;

//Begränsar hur många frågor en användare får ställa till Nova per timme
public sealed class AssistantRateLimiter
{
    //Konstanter
    private static readonly TimeSpan Window = TimeSpan.FromHours(1); //fönstrets längd

    //Beroenden
    private readonly IMemoryCache _cache; //var räknarna sparas (i serverns minne)
    private readonly TimeProvider _time; //vad klockan är
    private readonly int _maxQuestionsPerWindow; //gränsen, från appsettings
    //Lås - utan detta kan två frågor komma samtidigt från en användare. 
    private readonly object _lock = new();

    //Konstruktor
    public AssistantRateLimiter(IMemoryCache cache, TimeProvider time, int maxQuestionsPerWindow)
    {
        _cache = cache;
        _time = time;
        _maxQuestionsPerWindow = maxQuestionsPerWindow;
    }

    //Publik metod 
    //Försök konsumera en fråga - om true, frågan ställs, om false, gräns uppnådd
    public bool TryConsume(string userId)
    {
        var key = $"assistant-rate:{userId}"; //en egen räknare per användare
        var now = _time.GetUtcNow();

        lock(_lock) //bara en tråd åt gången i det här blocket
        {
            //1. Hämta användarens fönster (null om det inte finns eller gått ut)
            var window = _cache.Get<RateWindow>(key);

            //2. Inget fönster, eller fönstret gått ut -> starta ett nytt
            if(window is null || now >= window.StartedAt + Window)
                window = new RateWindow(now, 0);

            //3. Gränsen nådd --> Stoppa (räkna inte upp)
            if(window.Count >= _maxQuestionsPerWindow) return false;

            //4. Räkna upp och spara. Cachen tar bort posten automatiskt när fönstret går ut så det inte sparas för evigt i minnet.
            var updated = window with {Count = window.Count + 1};
            _cache.Set(key, updated, updated.StartedAt + Window);

            return true;
        }

    }

//Ett användarfönster: När det startade, och hur många frågor det använt hittils
//"record" + "with" ovan = skapar en kopia med ändrat count. 
    private sealed record RateWindow(DateTimeOffset StartedAt, int Count);

}