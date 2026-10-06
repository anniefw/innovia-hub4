using Innovia.Api.Common.Result;

namespace Innovia.Api.Features.Assistant.AskAssistant;


//Endpointen gör 3 saker innan openAI anropas:
// Inloggningskontrollen (RequireAuthorization) stoppar okända användare.
// Valideringen stoppar tomma, för långa eller manipulerade frågor.
// Först därefter körs Handlern och OpenAI-anropet.
// Ordningen går från billigast till dyrast. 
public static class Endpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app)
    {
        return app.MapPost("/ask", async (
            Request request, 
            Handler handler, 
            Validator validator, 
            CancellationToken ct
        ) =>
        {
            var validation = validator.Validate(request);
            if(!validation.IsValid)
                return validation.ToProblemResult();

                var answer = await handler.HandleAsync(request, ct);

                return Results.Ok(new Response(answer));
        });
    }
}