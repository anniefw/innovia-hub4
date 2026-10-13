using Innovia.Api.Common.Database;
using Innovia.Api.Common.Result;
using Microsoft.EntityFrameworkCore;

namespace Innovia.Api.Features.Sensors.ListSensors;

//Syfte: kolla vilka sensor som finns och i vilket rum de sitter
//För att veta behöver Handler inhämta info från 2 källor
//1) DeviceRegistry.Api vet Vilka sensorer som finns, och deras RoomId (ett GUID), men vet INTE vad rummet heter
// Din databas (AppDbContext) vet Vad varje rum heter (Resource.Name) men vet INTE Att sensorerna finns
public sealed class Handler
{
    private readonly DeviceRegistryClient _registry; //Pratar med deviceRegistry.Api via HTTP
    private readonly DeviceRegistryOptions _options; //vet att TenantSlug = "innovia" 
    private readonly AppDbContext _context;

    public Handler(DeviceRegistryClient registry, DeviceRegistryOptions options, AppDbContext context)
    {
        _registry = registry;
        _options = options;
        _context = context;
    }

    public async Task<Result<List<Response>>> HandleAsync(CancellationToken ct)
    {
        // STEG 1 + 2: Hämta tenant och enheter från DeviceRegistry.
        List<DeviceRegistryDevice> devices; //skapar en "tom låda" med en lista av devices - försöker sedan: 
        try
        {
            //Frågar registry vilken tenant heter TenantSlug(dvs innovia)
            var tenant = await _registry.GetTenantBySlugAsync(_options.TenantSlug, ct);    
            // tenant = [{ Id: dbdc9044…, Name: "Innovia Hub", Slug: "innovia" }]  eller null
            
            if(tenant is null)
                return Result<List<Response>>.Fail(SensorErrors.TenantNotConfigured);

            //Frågar registry vilka devices finns på den tenanten vi hämtade 
            devices = await _registry.ListDevicesAsync(tenant.Id, ct);
            // devices = [
            //   { Id: f2fd3c1d…, Serial: "dev-101", Model: "Acme CO2-Temp", RoomId: 01a07b3f…, … },
            //   { Id: b857ae38…, Serial: "dev-102", Model: "Acme CO2-Temp", RoomId: 01a07b3f-…, … },
            //   … totalt 10 st
            // ]

        } catch (HttpRequestException)
        {
            // Hit kommer vi om HTTP-anropet misslyckas helt (t.ex. "connection refused"),
            // eller om DeviceRegistry svarar med ett fel (500) → EnsureSuccessStatusCode() kastar.
            return Result<List<Response>>.Fail(SensorErrors.RegistryUnavailable);
        } catch(TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Result<List<Response>>.Fail(SensorErrors.RegistryUnavailable);
        }

        // STEG 3: Samla ihop alla RoomId som inte är null (Distinct – flera sensorer kan dela rum)
        // Gå igenom alla sensorer
        // behåll bara de som HAR ett RoomId (det kan vara null)
        // plocka ut själva RoomId:t
        // Ta bort dubbletter (om flera sensorer sitter i samma rum)
        // Gör om till en lista
        var roomIds = devices
            .Where(d => d.RoomId.HasValue) //filtrera bort sensorer som inte har ett rum
            .Select(d => d.RoomId!.Value) //Välj rummen som har Guid
            .Distinct() //ta bort dubeletter
            .ToList();
            // roomIds = [ 01a07b3f…, 01a07b40…, …, 10 olika rum-GUID ]

        // STEG 4: Hämta namnen för de rummen från _context.Resources i EN fråga
        // I tabellen Resources:
        // ta bara de rader vars Id finns i roomIds-listan
        // bygg en "uppslagsbok" (Dictionary): Id → Name
        // EF Core översätter detta till SQL: SELECT "Id", "Name" FROM "Resources" WHERE "Id" IN (01a07b3f…, 01a07b40…, …)
        var roomNames = await _context.Resources //Från resurser i databasen
            .AsNoTracking()
            .Where(r => roomIds.Contains(r.Id)) //hämta de rum som har Id
            .ToDictionaryAsync(r => r.Id, r => r.Name, ct); //lägg till dem i "uppslagsverk där de ska ha Id och Namn
            // roomNames = {
            //   01a07b3f… → "Mötesrum 1",
            //   01a07b40… → "Mötesrum 2",
            //   …
            // }

        // STEG 5: Mappa och sortera på serial
        // För varje sensor:
        // skapa en Response med sensorns uppgifter
        // RUMSNAMN:
        var sensors = devices
            .Select(d => new Response(
                d.Id,
                d.Serial,
                d.Model,
                d.Status,
                d.RoomId,
                //d.RoomId is Guid roomId är pattern matching: “om RoomId har ett värde, lägg det i variabeln roomId”
                d.RoomId is Guid roomId && roomNames.TryGetValue(roomId, out var name)
                ? name //OM sensorn har ett RoomId OCH det finns i uppslagsboken → använd namnet därifrån ["Mötesrum 1"]
                : "Okänt rum"  // ANNARS → "Okänt rum"
                ))
                .OrderBy(s => s.Serial) // Sortera listan på Serial (dev-101, dev-102, …)
                .ToList();
                // sensors = [
                //   { Id: f2fd3c1d…, Serial: "dev-101", Model: "Acme CO2-Temp", RoomName: "Mötesrum 1", … },
                //   { Id: b857ae38…, Serial: "dev-102", Model: "Acme CO2-Temp", RoomName: "Mötesrum 2", … },
                //   …
                // ]

        // STEG 6: returnera Result<List<Response>>.Ok(...)

        return Result<List<Response>>.Ok(sensors);
    }
}