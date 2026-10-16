using Innovia.Api.Common.Database;
using Innovia.Api.Common.Result;
using Microsoft.EntityFrameworkCore;

namespace Innovia.Api.Features.Sensors.ListSensors;

public sealed class Handler
{
    private readonly DeviceRegistryClient _registry;
    private readonly DeviceRegistryOptions _options;
    private readonly AppDbContext _context;

    public Handler(DeviceRegistryClient registry, DeviceRegistryOptions options, AppDbContext context)
    {
        _registry = registry;
        _options = options;
        _context = context;
    }

    public async Task<Result<List<Response>>> HandleAsync(CancellationToken ct)
    {
        List<DeviceRegistryDevice> devices;
        try
        {
            var tenant = await _registry.GetTenantBySlugAsync(_options.TenantSlug, ct);

            if (tenant is null)
                return Result<List<Response>>.Fail(SensorErrors.TenantNotConfigured);

            devices = await _registry.ListDevicesAsync(tenant.Id, ct);
        }
        catch (HttpRequestException)
        {
            return Result<List<Response>>.Fail(SensorErrors.RegistryUnavailable);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Result<List<Response>>.Fail(SensorErrors.RegistryUnavailable);
        }

        var roomIds = devices
            .Where(d => d.RoomId.HasValue)
            .Select(d => d.RoomId!.Value)
            .Distinct()
            .ToList();

        var rooms = await (
                from r in _context.Resources.AsNoTracking()
                join rt in _context.ResourceTypes.AsNoTracking()
                    on r.ResourceTypeId equals rt.Id
                where roomIds.Contains(r.Id)
                select new { r.Id, r.Name, TypeName = rt.Name }
            )
            .ToDictionaryAsync(x => x.Id, ct);

        var sensors = devices
            .Select(d =>
            {
                var room = d.RoomId is Guid roomId && rooms.TryGetValue(roomId, out var found)
                    ? found
                    : null;

                return new Response(
                    d.Id,
                    d.Serial,
                    d.Model,
                    d.Status,
                    d.RoomId,
                    room?.Name ?? "Okänt rum",
                    room?.TypeName ?? "Okänd"
                );
            })
            .OrderBy(s => s.Serial)
            .ToList();

        return Result<List<Response>>.Ok(sensors);

    }
}