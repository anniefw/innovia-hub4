using Innovia.Api.Common.Errors;
using Innovia.Api.Common.Result;

namespace Innovia.Api.Features.Assistant.AskAssistant;

public sealed class Validator
{
    private const int MaxQuestionLength = 500;
    private const int MaxQuestionsHistory = 20; 
    private const int MaxHistoryTextLength = 2000;

    private static readonly string [] AllowedRoles = ["user", "assistant"];

    public ValidationResult Validate (Request request)
    {
        var errors = new List<ValidationError>(); 

        if(string.IsNullOrWhiteSpace(request.Question))
            errors.Add(new ValidationError(nameof(request.Question), "Frågan får inte vara tom"));

        if(request.Question.Length > MaxQuestionLength)
            errors.Add(new ValidationError(nameof(request.Question), $"Frågan får inte överstiga {MaxQuestionLength} tecken."));

        if(request.History is not null)
        {
            if(request.History.Count > MaxQuestionsHistory)
                errors.Add(new ValidationError(nameof(request.Question), $"Historiken får inte överstiga {MaxQuestionsHistory} meddelanden."));
            
            foreach (var turn in request.History)
            {
                // Bara "user" och "assistant". Stoppar försök att skicka "system".
                // (Handlern har dessutom sitt eget skydd: dubbel säkerhet.)
                if (!AllowedRoles.Contains(turn.Role))
                {
                    errors.Add(new ValidationError(nameof(request.History),
                        $"Ogiltig roll '{turn.Role}'. Tillåtna: user, assistant."));
                    break;   
                }

                if (turn.Text?.Length > MaxHistoryTextLength)
                {
                    errors.Add(new ValidationError(nameof(request.History),
                        $"Ett meddelande i historiken är längre än {MaxHistoryTextLength} tecken."));
                    break;
                }
            }
        }

        return errors.Count == 0
        ? ValidationResult.Success()
        : ValidationResult.Fail(errors);

    }
}