using Microsoft.AspNetCore.Identity;

namespace Innovia.Api.Features.Assistant.AskAssistant;

public sealed record Response(
    string Answer
);