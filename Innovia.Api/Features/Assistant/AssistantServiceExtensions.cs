using Innovia.Api.Common.Auth;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Innovia.Api.Features.Assistant;
    public static class AssistantServiceExtensions
    {
        //Tar emot IConfiguration för att kunna läsa modell och nyckel
    public static IServiceCollection AddAssistantFeature (this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AssistantContextBuilder>();
        services.AddScoped<AskAssistant.Handler>();
        services.AddScoped<AskAssistant.Validator>();

        //1. Läs konfiguration. IConfiguration slår emot appsetting.json, user secrets och miljövariabler
        var apiKey = configuration["OpenAI:ApiKey"]; //fr User Secrets
        var model = configuration["OpenAI:Model"]; //fr appsettings

        //2. Kontrollera att värdena finns
        if(string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI:ApiKey saknas. Kör: dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"");

        if(string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("OpenAI:Model saknas i appsettings.json");

        //3. Registrera IChatClient
        services.AddSingleton<IChatClient>(new ChatClient(model,apiKey).AsIChatClient());


        return services;
    }
        
    public static IEndpointRouteBuilder MapAssistantEndpoints (this IEndpointRouteBuilder app)
    {
        //routegrupp: All endpoints här får adess som börjar med /assistant
        var group = app.MapGroup("/assistant").WithTags("Assistant");

        AskAssistant.Endpoint.Map(group)
            .RequireAuthorization();

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
