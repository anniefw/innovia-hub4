using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace Innovia.Api.Features.Assistant.AskAssistant;

public sealed class Handler
{
    private const int maxHistoryMessages = 6;  
    public const string FallBackPhrase = "Det hittar jag tyvärr ingen information om";

    private readonly IChatClient _chatClient; 
    private readonly AssistantContextBuilder _contextBuilder; 
    private readonly ILogger<Handler> _logger; 


    public Handler(IChatClient chatClient, AssistantContextBuilder contextBuilder, ILogger<Handler> logger)
    {
        _chatClient = chatClient;
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    public async Task<string> HandleAsync(Request request, CancellationToken ct)
    {
        var messages = await BuildMessagesAsync(request, ct);
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);

        LogIfUnanswered(request.Question, response.Text);

        return response.Text;
    }
    public async IAsyncEnumerable<string> StreamAsync(
        Request request,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var messages = await BuildMessagesAsync(request, ct);

        var fullAnswer = new StringBuilder();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct ))
        {
            if(string.IsNullOrEmpty(update.Text))
                continue;

            fullAnswer.Append(update.Text);

            yield return update.Text;
            
        }

        LogIfUnanswered(request.Question, fullAnswer.ToString());
       
    }

    private void LogIfUnanswered(string question, string answer)
    {
        if (!answer.Contains(FallBackPhrase, StringComparison.OrdinalIgnoreCase))
        return;

        _logger.LogInformation("Nova kunde inte svara på frågan: {Question}", question);

    }

    private async Task<List<ChatMessage>> BuildMessagesAsync(Request request, CancellationToken ct)
    {
        var systemPrompt = await _contextBuilder.ReadSystemPromptAsync(ct);  
        var context = await _contextBuilder.BuildAsync(ct);                 

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"{systemPrompt}\n\n{context}")
        };

        var history = (request.History ?? []).TakeLast(maxHistoryMessages);

        foreach (var turn in history)
        {
            var role = turn.Role == "assistant" ? ChatRole.Assistant : ChatRole.User;
            messages.Add(new ChatMessage(role, turn.Text));
        }

        messages.Add(new ChatMessage(ChatRole.User, request.Question));

        return messages;
    }
}