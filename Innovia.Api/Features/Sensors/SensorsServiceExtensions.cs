using System.Linq.Expressions;
using Innovia.Api.Common.Auth;
using Microsoft.VisualBasic;

namespace Innovia.Api.Features.Sensors;

public static class SensorsServiceExtensions
{
    //Del 1: Registrera tjänster i DI-Containern (som anropas från Program)

    public static IServiceCollection AddSensorsFeature(this IServiceCollection services, IConfiguration configuration)
    {
        //1a. Läs ur appsettings.json
        var baseUrl = configuration["DeviceRegistry:BaseUrl"];
        var tenantSlug = configuration["DeviceRegistry:TenantSlug"];

        //1b. Kontrollera att värdena finns, annars krascha dirr med tydligt meddelande
        if(string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Device:RegistryBaseUrl is not configured correct in appsettings.json");
        
        if(string.IsNullOrWhiteSpace(tenantSlug))
            throw new InvalidOperationException("DeviceRegistry:TenantSlug is not configured correctly in appsettings.json");

        //1c. Registrera inställningarna som singleton 
        services.AddSingleton(new DeviceRegistryOptions(baseUrl,tenantSlug));

        //1d. Registrera den typade klienten
        //AddHttpClient gör 2 saker: 
        // 1) registrera DeviceRegistryClient i DI och 
        // 2) ger den en HttpClient med BaseAdress redan satt, där IHttpClient 
        //återanvänder nätverksanslutningarna åt oss 

        services.AddHttpClient<DeviceRegistryClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl); //http://localhost:5101/
            client.Timeout = TimeSpan.FromSeconds(5); //väntar inte för evigt
        });

        //1e. Registrera handlern
        services.AddScoped<ListSensors.Handler>();

        return services;

    }

    //2. Registrera Endpoints
    //Skapa gruppen sensors med tagg "Sensors" syns i Scalas
    //Koppla in ListSensors-Endpointen och kräv adminroll

    public static IEndpointRouteBuilder MapSensorsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sensors").WithTags("Sensors");

        ListSensors.Endpoint.Map(group)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return group;
    }

    
}