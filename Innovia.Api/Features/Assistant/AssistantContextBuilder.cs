using Innovia.Api.Common.Database;

namespace Innovia.Api.Features.Assistant;

public sealed class AssistantContextBuilder
{
    //Konstanter
    private const string KnowledgeFolder = "Features/Assistant";
    private const string KnowledgeFileName = "knowledge.md";

    //Beroenden
    private readonly AppDbContext _context;

    //Konstruktor
    public AssistantContextBuilder (AppDbContext context)
    {
        _context = context;
    }

    //Publik metod som anropas utifrån
     public async Task<string> BuildAsync (CancellationToken ct)
    {
        var knowledge = await ReadKnowledgeAsync(ct); 
        return knowledge;
    }

    //Privata hjälpmetoder
    private async Task<string> ReadKnowledgeAsync(CancellationToken ct)
    {
        //1. Sökvägen
        var path = Path.Combine(AppContext.BaseDirectory, KnowledgeFolder, KnowledgeFileName);

        //2. Kontrollera att filen finns
        if(!File.Exists(path))
        {
            throw new FileNotFoundException($"Kunskapsfilen hittades inte på `{path}`. Kontrollera sökväg och output.");
        }

        //3. Läs filen
        var content = await File.ReadAllTextAsync(path, ct);

        //4. Returnera texten
        return content.Trim();
    }

    // private async Task<string> BuildOpeningHoursAsync(CancellationToken ct)
    // {
            
     // }

    // private async Task<string> BuildResourceAsync(CancellationToken ct)
    // {
            
    // }

}
