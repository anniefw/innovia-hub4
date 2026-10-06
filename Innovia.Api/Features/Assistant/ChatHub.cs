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

    public ChatHub(Handler handler, Validator validator)
    {
        _handler = handler;
        _validator = validator;

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

        //2. Strömma svaret - loopa över handlers svar och ge svar bitvis
        await foreach(var chunk in _handler.StreamAsync(request, ct))
        {
            yield return chunk;
        // När loopen är klar avslutas strömmen automatiskt och
        // klienten får signalen "complete".
        }
    }
    
}