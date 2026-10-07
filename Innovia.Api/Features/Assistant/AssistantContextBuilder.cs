using System.Globalization;
using System.Text;
using Innovia.Api.Common.Database;
using Innovia.Api.Common.Database.Entities;
using Innovia.Api.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Innovia.Api.Features.Assistant;

//Klassens uppgift: Sätta ihop all text som en sträng till Nova 
public sealed class AssistantContextBuilder
{
    //Konstanter

    private const string AssistantFolder = "Features/Assistant";
    private const string KnowledgeFileName = "knowledge.md";
    private const string SystemPromptFileName = "systemprompt.md";

    //Statiska fält
    //Sätter veckodagarna i svensk ordning
    private static readonly DayOfWeek[] SwedishWeekOrder = [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    //ger måndag/oktober istället för Monday/October
    private static readonly CultureInfo Swedish = new ("sv-SE");

    //Beroenden
    private readonly AppDbContext _context;

    //Konstruktor
    public AssistantContextBuilder (AppDbContext context)
    {
        _context = context;
    }

    //Publik metod som anropas från de andra klasserna

    public Task<string> ReadSystemPromptAsync(CancellationToken ct) =>
        ReadAssistantFileAsync(SystemPromptFileName, ct);
     public async Task<string> BuildAsync (CancellationToken ct)
    {
        var now = BuildCurrentTime(); //del 0: vad är klockan nu?
        var knowledge = await ReadAssistantFileAsync(KnowledgeFileName, ct); //del 1: texten fron knowledge.md
        var openingHours = await BuildOpeningHoursAsync(ct); //del 2: bokningsbara tider från DB
        var resources = await BuildResourcesAsync(ct); //del 3: resurser med status (livedata) från DB

        //Sätter ihop infon till text för AI

                return $"""
            --- AKTUELL TID ---
            {now}

            --- ALLMÄN INFORMATION ---
            {knowledge}

            --- BOKNINGSBARA TIDER PER RESURSTYP ---
            {openingHours}

            --- RESURSER OCH AKTUELL STATUS ---
            {resources}
            """;                         


    }


    //Metod som förklarar för Nova vilken dag och tid det är (svensk tid)
    private static string BuildCurrentTime()
    {
        //Konvertera till svensk tid
        var swedishNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, SwedenTimeZone.Instance);
        
        //Formatera på svenska
        return swedishNow.ToString("dddd d MMMM yyyy 'kl.' HH:mm", Swedish);
    }

    //Privata hjälpmetoder

    //Hämtar, (vid behov errorhanterar), läser och returnerar assistant-texter till Nova
    private async Task<string> ReadAssistantFileAsync(string fileName, CancellationToken ct)
    {
        //1. Sökvägen
        var path = Path.Combine(AppContext.BaseDirectory, AssistantFolder, fileName);

        //2. Kontrollera att filen finns
        if(!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Filen '{fileName}' hittades inte på '{path}'. " +
                "Kontrollera att den finns i Features/Assistant och att .csproj kopierar den.");
        }

        //3. Läs filen
        var content = await File.ReadAllTextAsync(path, ct);

        //4. Returnera texten
        return content.Trim();
    }

    //Bygger öppningstider till Nova från databasen
    private async Task<string> BuildOpeningHoursAsync(CancellationToken ct)
    {
        //1. Hämta resurstyperna
        var resourceTypes = await _context.ResourceTypes
        .AsNoTracking()
        .OrderBy(rt => rt.Name)
        .ToListAsync(ct);

        //2.Hämta ALLA regler
        var rules = await _context.AvailabilityRules
        .AsNoTracking()
        .ToListAsync(ct);

        //3. Bygg texten
        var sb = new StringBuilder();

        foreach(var type in resourceTypes)
        {
            //3a. rubrikrad till text
            sb.AppendLine($"{type.Name} (max {FormatDuration(type.MaxDurationMinutes)} per bokning," + $" kan bokas max {type.MaxAdvanceDays} dagar i förväg):");

            
            foreach(var day in SwedishWeekOrder)
            {
                //3b. Hitta regeln för den här typen och den här dagen
                var rule = rules.FirstOrDefault(r => 
                r.ResourceTypeId == type.Id &&
                r.DayOfWeek == day);

                var dayName = Swedish.DateTimeFormat.GetDayName(day);

                //3c. Om "null" - kan ej boka
                if(rule is null)
                {
                    sb.AppendLine($"- {dayName}: stängt, kan inte bokas.");
                } else //3d. Regel finns, ge regelinfo
                {
                    var opens = rule.OpensAt.ToString("HH:mm");
                    var closes = rule.ClosesAt.ToString("HH:mm");
                    sb.AppendLine($"- {dayName}: {opens} - {closes}");
                    
                }
            }
            sb.AppendLine(); //rom rad emellean resurstyperna
        }

        return sb.ToString().Trim(); //4. returnera och stäng utan sista tomrad.
            
    }

    //Hjälpmetod:
    private static string FormatDuration(int minutes) => 
        minutes % 60 == 0
        ? $"{minutes/60} h"
        : $"{minutes} min";

    //Bygger text utifrån vilka resurser som finns och status just nu (live-data)
    private async Task<string> BuildResourcesAsync(CancellationToken ct)
    {
        //1. Hämta resurstyperna
        var resourcetypes = await _context.ResourceTypes
            .AsNoTracking()
            .OrderBy(rt => rt.Name)
            .ToListAsync(ct);


        //2. Hämta alla resurser (som inte är arkiverade)
        var resources = await _context.Resources
            .AsNoTracking()
            .Where(r => r.Status != ResourceStatus.Archived) //filtrera bort arkiverade
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        //3. Bygg texten
        var sb = new StringBuilder(); //"Anteckningsblocket"

        foreach(var type in resourcetypes) //en sektion per resurstyp
        {

            //3a. Plocka ut resurserna som hör till den här typen
            var resourcesOfType = resources
                .Where(rot => rot.ResourceTypeId == type.Id)
                .ToList();

            //3b. Om inga resurser av den typen finns, hoppa till nästa typ
            if (resourcesOfType.Count == 0) continue;

            //3c. Rubrikrad
            sb.AppendLine($"{type.Name}:");

            //3d. Dela upp typens resurser i grupper efter status och sortera grupperna
            var statusGroups = resourcesOfType
            .GroupBy(r => r.Status)
            .OrderBy(g => g.Key); //Key kommer från status

            foreach (var group in statusGroups) //en rad per status

            //3e. Gör om gruppens resurser till kommaseparerad sträng
            {
                var names = string.Join(", ", group.Select(g => g.Name));

            //3f. Skriv raden
                sb.AppendLine($"-{DescribeStatus(group.Key)}: {names}");
            }
            sb.AppendLine(); //tom rad mellan resurstyperna
        }

        //4. Returnera texten
        return sb.ToString().Trim(); //returnerar ifyllt "anteckningsblock" som sträng och utan sista tomrad
    }

    //Hjälpmetod: översätter status till info som AI och medlem förstår
    private static string DescribeStatus(ResourceStatus status) => status switch
    {
        ResourceStatus.Online => "Tillgängliga att boka",
        ResourceStatus.Offline => "Ur drift (kan tyvärr inte bokas i nuläget)",
        ResourceStatus.Maintenance => "Underhåll (kan tyvärr inte bokas i nuläget)",
        _ => status.ToString() //_ fångar allt annat, säkerhet om fler saker läggs till. 
    };

}
