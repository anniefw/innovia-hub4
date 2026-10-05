using Innovia.Api.Common.Auth;

namespace Innovia.Api.Features.Occupancy;

public static class OccupancyServiceExtensions
{
    public static IServiceCollection AddOccupancyFeature(this IServiceCollection services)
    {
        services.AddScoped<GetCurrentOccupancy.Handler>();
        services.AddScoped<GetOccupancyByRange.Handler>();

        return services;
    }

    public static IEndpointRouteBuilder MapOccupancyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/occupancy").WithTags("Occupancy");

        GetCurrentOccupancy.Endpoint.Map(group)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);
        
        GetOccupancyByRange.Endpoint.Map(group)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return group;
    }
}


