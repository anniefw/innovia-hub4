using Innovia.Api.Common.Auth;

namespace Innovia.Api.Features.Assistant;
    public static class AssistantServiceExtensions
    {
    public static IServiceCollection AddAssistantFeature (this IServiceCollection services)
    {
        services.AddScoped<AssistantContextBuilder>();

        return services;
    }
        
    public static IEndpointRouteBuilder MapAssistantEndpoints (this IEndpointRouteBuilder app)
    {
        //routegrupp: All endpoints här får adess som börjar med /assistant
        var group = app.MapGroup("/assistant").WithTags("Assistant");

        //Tillfällig
        group.MapGet("/context", async (AssistantContextBuilder builder, CancellationToken ct) =>
        {
            var text = await builder.BuildAsync(ct);


            return Results.Text(text, "text/plain; charset=utf-8");
            //charset=utf-8 gör så att å ä ö inte blir konstiga tecken i webbläsaren
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return group;
        
    }


}
