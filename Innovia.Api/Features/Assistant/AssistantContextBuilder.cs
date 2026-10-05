using System.Globalization;
using System.Text;
using Innovia.Api.Common.Database;
using Innovia.Api.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Innovia.Api.Features.Assistant;

//Klassens uppgift: Sätta ihop all text som en sträng till Nova 
public sealed class AssistantContextBuilder
{
    //Konstanter
    private const string KnowledgeFolder = "Features/Assistant";
    private const string KnowledgeFileName = "knowledge.md";

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
     public async Task<string> BuildAsync (CancellationToken ct)
    {
        var now = BuildCurrentTime(); //del 0: vad är klockan nu?
        var knowledge = await ReadKnowledgeAsync(ct); //del 1: texten fron knowledge.md
        var openingHours = await BuildOpeningHoursAsync(ct); //del 2: bokningsbara tider från databasen

        //Sätter ihop infon till text för AI

                return $"""
            --- AKTUELL TID ---          
            {now}

            ---ALLMÄN INFORMATION---
            {knowledge}


            ---BOKNINGSBARA TIDER PER RESURSTYP---
            {openingHours}
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

    //Hämtar, (vid behov errorhanterar), läser och returnerar texten knowledge.md till Nova
    private async Task<string> ReadKnowledgeAsync(CancellationToken ct)
    {
        //1. Sökvägen
        var path = Path.Combine(AppContext.BaseDirectory, KnowledgeFolder, KnowledgeFileName);

        //2. Kontrollera att filen finns
        if(!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Kunskapsfilen hittades inte på '{path}'. " +
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
        .ToListAsync();

        //2.Hämta ALLA regler
        var rules = await _context.AvailabilityRules
        .AsNoTracking()
        .ToListAsync();

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
                    sb.AppendLine($"- {dayName}: kan inte bokas.");
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

    // private async Task<string> BuildResourceAsync(CancellationToken ct)
    // {
            
    // }

}
