namespace Innovia.Api.Features.Assistant.AskAssistant;

public sealed record Request(
    string Question, 
    List<ChatHistory>? History
    );

public sealed record ChatHistory(
    string Role, 
    string Text
    );