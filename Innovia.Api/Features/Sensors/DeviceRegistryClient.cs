using System.Net;

namespace Innovia.Api.Features.Sensors;

//Typad HttpClient: All kommunikation med DeviceRegistry.Api  samlas här
// HttpClient:en (med BaseAddress) injiceras av IHttpClientFactory via AddHttpClient<DeviceRegistryClient>().
public sealed class DeviceRegistryClient
{
        private readonly HttpClient _http;

        public DeviceRegistryClient(HttpClient http)
    {
        _http = http;
    }

    //GET api/tenant/by-slug/{slug}
    //Returnerar tenanten (eller null om ej slug finns)

    public async Task<DeviceRegistryTenant?> GetTenantBySlugAsync(string slug, CancellationToken
     ct)
    {
        var response = await _http.GetAsync($"api/tenants/by-slug/{slug}", ct);

        if(response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DeviceRegistryTenant>(ct);
    }

    //GET api/tenants/{tenantId}/devices
    //Returnerar alla enheter för tenanten (tom lista om ej finns)
    public async Task<List<DeviceRegistryDevice>> ListDevicesAsync(Guid tenantId, CancellationToken ct)
    {
        var devices = await _http.GetFromJsonAsync<List<DeviceRegistryDevice>>($"api/tenants/{tenantId}/devices", ct);

        return devices ?? [];
    }
}

public sealed record DeviceRegistryTenant (Guid Id, string Name, string Slug);

public sealed record DeviceRegistryDevice(
    Guid Id,
    Guid TenantId,
    Guid? RoomId,
    string Model,
    string Serial, 
    string Status
);