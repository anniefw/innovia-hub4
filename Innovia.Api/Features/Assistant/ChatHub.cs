using System.Runtime.CompilerServices;   
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.SignalR;
using Innovia.Api.Features.Assistant.AskAssistant;      

namespace Innovia.Api.Features.Assistant;

[Authorize]
public class ChatHub : Hub
{
    //beroende
    private readonly Handler _handler;
    private readonly Validator _validator;
    private readonly AssistantRateLimiter _assistantRateLimiter;

    public ChatHub(Handler handler, Validator validator, AssistantRateLimiter assistantRateLimiter)
    {
        _handler = handler;
        _validator = validator;
        _assistantRateLimiter = assistantRateLimiter;

    }

    //Hub-metod: frontend anropar med connectionstream "ask"
    //Returtypen IAsyncEnumerable gör det till streaming-metod: SignalR loopar över den och yieldar return till klienten vid varje bit.
    //Requesten är samma typ som POST, SignalR gör om Json-frontend-anropet till en request.
    public async IAsyncEnumerable<string> Ask(Request request, [EnumeratorCancellation] CancellationToken ct)
    {
        //1. Validera
        var validation = _validator.Validate(request);

        if(!validation.IsValid)
            throw new HubException("Frågan är ogiltig. Den får inte vara tom, och får vara max 500 tecken.");

        //2. Kolla rate limit
        var userId = Context.UserIdentifier; //den inloggade användarens id

        //ska inte gå att vara null pga [Athorize] kräver inloggning, men hellre säkra att riskera släppa igenom okänd anv
        if(userId is null || !_assistantRateLimiter.TryConsume(userId))
        {
            throw new HubException(AssistantRateLimiter.LimitReachedMessage);
        }

        //3. Strömma svaret - loopa över handlers svar och ge svar bitvis
        await foreach(var chunk in _handler.StreamAsync(request, ct))
        {
            yield return chunk;
        // När loopen är klar avslutas strömmen automatiskt och
        // klienten får signalen "complete".
        }
    }
    
}