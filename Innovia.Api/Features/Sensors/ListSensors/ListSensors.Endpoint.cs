using Innovia.Api.Common.Result;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Innovia.Api.Features.Sensors.ListSensors;

public static class Endpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app)
    {
        return app.MapGet("/", async (
            Handler handler,
            CancellationToken ct
        ) =>
        {
            var result = await handler.HandleAsync(ct);
            return result.ToHttpResponse();
        });
    }
}