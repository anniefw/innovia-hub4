using Innovia.Api.Common.Auth;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI.Chat;

namespace Innovia.Api.Features.Assistant;
    public static class AssistantServiceExtensions
    {
    public static IServiceCollection AddAssistantFeature (this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AssistantContextBuilder>();
        services.AddScoped<AskAssistant.Handler>();
        services.AddScoped<AskAssistant.Validator>();

        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        var apiKey = configuration["OpenAI:ApiKey"]; 
        var model = configuration["OpenAI:Model"]; 
        var maxQuestionsPerHour = configuration.GetValue("Assistant:MaxQuestionsPerHour", 20);

        if(string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI:ApiKey saknas. Kör: dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"");

        if(string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("OpenAI:Model saknas i appsettings.json");

        services.AddSingleton<IChatClient>(new ChatClient(model,apiKey).AsIChatClient());

        services.AddSingleton(sp => new AssistantRateLimiter(
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<TimeProvider>(),
            maxQuestionsPerHour));

        return services;
    }
        
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHub<ChatHub>("/hubs/chat");

        var group = app.MapGroup("/assistant")
            .WithTags("Assistant")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        AskAssistant.Endpoint.Map(group);

        group.MapGet("/context", async (AssistantContextBuilder builder, CancellationToken ct) =>
        {
            var text = await builder.BuildAsync(ct);
            return Results.Text(text, "text/plain; charset=utf-8");
        });

        return app;
    }
}
