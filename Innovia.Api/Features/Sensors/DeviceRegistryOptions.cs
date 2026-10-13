namespace Innovia.Api.Features.Sensors;

//Inställningar för DeviceRegistry, lästa från appsettings.json
public sealed record DeviceRegistryOptions(string BaseUrl, string TenantSlug);