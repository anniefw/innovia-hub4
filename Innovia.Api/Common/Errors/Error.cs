using Microsoft.AspNetCore.Http.HttpResults;

namespace Innovia.Api.Common.Errors;

public record Error(string Message, ErrorType Type)
{
    public static Error Failure(string message) => 
        new(message, ErrorType.Failure);
    public static Error NotFound(string message) => 
        new(message, ErrorType.NotFound);
    public static Error Conflict(string message) => 
        new(message, ErrorType.Conflict);
    public static Error Forbidden(string message) =>
        new(message, ErrorType.Forbidden);
    public static Error Validation(string message) =>
        new(message, ErrorType.Validation);
    public static Error Unavailable(string message) => 
        new(message, ErrorType.Unavailable);
}

public enum ErrorType
{
    Failure,
    NotFound,
    Conflict,
    Forbidden,
    Validation,
    Unavailable
}

public static class ErrorExtensions
{
    public static IResult ToProblemResult(this Error error) => error.Type switch
    {
        ErrorType.Failure => Results.Problem(
            title: "Internal Server Error",
            detail: error.Message,
            statusCode: StatusCodes.Status500InternalServerError
        ),
        ErrorType.NotFound => Results.Problem(
            title: "Not Found",
            detail: error.Message,
            statusCode: StatusCodes.Status404NotFound
        ),
        ErrorType.Conflict => Results.Problem(
            title: "Conflict",
            detail: error.Message,
            statusCode: StatusCodes.Status409Conflict
        ),
        ErrorType.Forbidden => Results.Problem(
            title: "Forbidden",
            detail: error.Message,
            statusCode: StatusCodes.Status403Forbidden
        ),
        ErrorType.Validation => Results.Problem(
            title: "Bad Request",
            detail: error.Message,
            statusCode: StatusCodes.Status400BadRequest
        ),
        ErrorType.Unavailable => Results.Problem(
            title: "Service Unavailable",
            detail: error.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable
        ),
        _ => throw new ArgumentOutOfRangeException(nameof(error.Type), "Unmapped ErrorType")
    };
}