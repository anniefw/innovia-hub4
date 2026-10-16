using System.Linq.Expressions;
using Innovia.Api.Common.Auth;
using Microsoft.VisualBasic;

namespace Innovia.Api.Features.Sensors;

public static class SensorsServiceExtensions
{
    public static IServiceCollection AddSensorsFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["DeviceRegistry:BaseUrl"];
        var tenantSlug = configuration["DeviceRegistry:TenantSlug"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Device:RegistryBaseUrl is not configured correct in appsettings.json");

        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new InvalidOperationException("DeviceRegistry:TenantSlug is not configured correctly in appsettings.json");

        services.AddSingleton(new DeviceRegistryOptions(baseUrl, tenantSlug));


        services.AddHttpClient<DeviceRegistryClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddScoped<ListSensors.Handler>();

        return services;

    }
    public static IEndpointRouteBuilder MapSensorsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sensors").WithTags("Sensors");

        ListSensors.Endpoint.Map(group)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return group;
    }

}