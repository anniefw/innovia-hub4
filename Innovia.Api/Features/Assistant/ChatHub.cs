using System.Runtime.CompilerServices;   
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.SignalR;
using Innovia.Api.Features.Assistant.AskAssistant;      

namespace Innovia.Api.Features.Assistant;

[Authorize]
public class ChatHub : Hub
{
    private readonly Handler _handler;
    private readonly Validator _validator;
    private readonly AssistantRateLimiter _assistantRateLimiter;

    public ChatHub(Handler handler, Validator validator, AssistantRateLimiter assistantRateLimiter)
    {
        _handler = handler;
        _validator = validator;
        _assistantRateLimiter = assistantRateLimiter;

    }
    public async IAsyncEnumerable<string> Ask(Request request, [EnumeratorCancellation] CancellationToken ct)
    {
        var validation = _validator.Validate(request);

        if(!validation.IsValid)
            throw new HubException("Frågan är ogiltig. Den får inte vara tom, och får vara max 500 tecken.");

        var userId = Context.UserIdentifier; 
        if(userId is null || !_assistantRateLimiter.TryConsume(userId))
        {
            throw new HubException(AssistantRateLimiter.LimitReachedMessage);
        }

        await foreach(var chunk in _handler.StreamAsync(request, ct))
        {
            yield return chunk;
        }
    }
    
}