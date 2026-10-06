using Microsoft.Extensions.AI;

namespace Innovia.Api.Features.Assistant.AskAssistant;

//Handlerns uppgift: ta emot en fråga, bygger ihop all information Nova behöver veta, returnerar svaret som sträng
public sealed class Handler
{
    //Konstanter
    private const int maxHistoryMessages = 6; //blir dyrt med för många. 

    //Beroenden
    private readonly IChatClient _chatClient; //vägen till AI
    private readonly AssistantContextBuilder _contextBuilder; //texterna + systemprompt

    //Konstruktor
    public Handler(IChatClient chatClient, AssistantContextBuilder contextBuilder)
    {
        _chatClient = chatClient;
        _contextBuilder = contextBuilder;
    }

    public async Task<string> HandleAsync(Request request, CancellationToken ct)
    {
        //1. Hämta instruktionerna och contex
        var systemPrompt = await _contextBuilder.ReadSystemPromptAsync(ct); //HUR Nova ska bete sig
        var context = await _contextBuilder.BuildAsync(ct); //VAD hon vet

        //2. Bygg meddelandelistan
            //2a. Systemmeddelande: instruktionerna först, sen frågan
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"{systemPrompt}\n\n{context}")
        };
            //2b. Historiken - bara de senaste meddelandena, "request.History ?? []" = om History är null, använd en tom lista
            //(annars kraschar TakeLast med NullReferenceException vid första frågan).
        var history = (request.History ?? []).TakeLast(maxHistoryMessages);

        foreach (var turn in history)
            //översätt rollen från text(frontend) till ChatRole(AI-biblotek)
            // SÄKERHET: bara exakt "assistant" blir Assistant, ALLT annat blir User.
            // Skickar någon "system" från webbläsaren blir det alltså ett vanligt
            // user-meddelande. Bara backend kan skapa systemmeddelandet.
        {
            var role = turn.Role == "assistant" ? ChatRole.Assistant : ChatRole.User;

            messages.Add(new ChatMessage(role, turn.Text));
        }

            //2c.Frågan som ställs nu
            messages.Add(new ChatMessage(ChatRole.User, request.Question));

        //3. Skicka meddelandelistan till AI (väntar in hela svaret)
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);

        //4. Returnera svaret som sträng
        return response.Text; 
    }
    
}