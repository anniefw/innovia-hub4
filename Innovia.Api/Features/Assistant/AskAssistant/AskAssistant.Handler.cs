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
    // ── Publik metod: helt svar på en gång ────────────────────────────────────
    // Används av POST /assistant/ask (felsökning i Scalar) och av testerna.
    public async Task<string> HandleAsync(Request request, CancellationToken ct)
    {
        // 1. Bygg meddelandelistan (gemensam logik, se nedan)
        var messages = await BuildMessagesAsync(request, ct);

        // 2. Skicka till AI:n och VÄNTA på hela svaret
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);

        // 3. Returnera all text
        return response.Text;
    }

    // ── Privat hjälpmetod: bygger meddelandelistan ────────────────────────────
    // UTBRUTEN från HandleAsync så att StreamAsync (4.2) kan använda samma logik.
    // All säkerhet kring roller finns därmed på ETT ställe.
    private async Task<List<ChatMessage>> BuildMessagesAsync(Request request, CancellationToken ct)
    {
        // ── a. Hämta instruktioner och kontext ────────────────────────────────
        var systemPrompt = await _contextBuilder.ReadSystemPromptAsync(ct);  // HUR Nova ska bete sig
        var context = await _contextBuilder.BuildAsync(ct);                  // VAD Nova vet

        // ── b. Systemmeddelandet först: instruktioner, sedan information ──────
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"{systemPrompt}\n\n{context}")
        };

        // ── c. Historiken: bara de senaste meddelandena ───────────────────────
        var history = (request.History ?? []).TakeLast(maxHistoryMessages);

        foreach (var turn in history)
        {
            // SÄKERHET: bara exakt "assistant" blir Assistant, ALLT annat blir User.
            // Bara backend kan skapa systemmeddelandet.
            var role = turn.Role == "assistant" ? ChatRole.Assistant : ChatRole.User;
            messages.Add(new ChatMessage(role, turn.Text));
        }

        // ── d. Sist: frågan som ställs nu ─────────────────────────────────────
        messages.Add(new ChatMessage(ChatRole.User, request.Question));

        return messages;
    }
}