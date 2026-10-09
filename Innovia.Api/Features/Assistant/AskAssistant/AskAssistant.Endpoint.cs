using Innovia.Api.Common.Auth;
using Innovia.Api.Common.Result;

namespace Innovia.Api.Features.Assistant.AskAssistant;
public static class Endpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app)
    {
        //För felsökning
        return app.MapPost("/ask", async (
            Request request, 
            Handler handler, 
            Validator validator, 
            AssistantRateLimiter assistantRateLimiter,
            ICurrentUser currentUser,
            CancellationToken ct
        ) =>
        {
            var validation = validator.Validate(request);
            if(!validation.IsValid)
                return validation.ToProblemResult();

            var userId = currentUser.UserId!.Value.ToString();

            if (!assistantRateLimiter.TryConsume(userId))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "För många frågor",
                    detail: AssistantRateLimiter.LimitReachedMessage
                );
            }
            
            var answer = await handler.HandleAsync(request, ct);

            return Results.Ok(new Response(answer));
        });
    }
}