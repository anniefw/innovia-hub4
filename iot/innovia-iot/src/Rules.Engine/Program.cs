// NOTE to maintainers:
// This file scaffolds a minimal Rules Engine so teams can plug in rule evaluation
// without first wiring all the plumbing. The plumbing (config, DbContexts, SignalR
// client, background worker loop) is in place – the actual rule logic is NOT.
// Packages you likely need in the .csproj (add via PackageReference):
//   - Microsoft.EntityFrameworkCore
//   - Npgsql.EntityFrameworkCore.PostgreSQL
//   - Microsoft.AspNetCore.SignalR.Client
//
// Connection strings are read from appsettings.json or environment variables:
//   - ConnectionStrings:RulesDb  (default: Host=localhost;Username=postgres;Password=password;Database=innovia_rules)
//   - ConnectionStrings:IngestDb (default: Host=localhost;Username=postgres;Password=password;Database=innovia_ingest)
// Hub URL:
//   - Realtime:HubUrl (default: http://localhost:5103/hub/telemetry)
//
// TODOs are intentionally terse and technical; treat them as guidance...

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR.Client;
var builder = WebApplication.CreateBuilder(args);

// --- Logging ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Trim noisy EF Core SQL logs
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);

// --- Config (with sensible defaults for local dev) ---
var rulesConn = builder.Configuration.GetConnectionString("RulesDb")
    ?? "Host=localhost;Username=postgres;Password=password;Database=innovia_rules";
var ingestConn = builder.Configuration.GetConnectionString("IngestDb")
    ?? "Host=localhost;Username=postgres;Password=password;Database=innovia_ingest";
var hubUrl = builder.Configuration.GetSection("Realtime")["HubUrl"]
    ?? "http://localhost:5103/hub/telemetry";

// --- Services ---
builder.Services.AddDbContext<RulesDbContext>(o => o.UseNpgsql(rulesConn));
builder.Services.AddDbContext<IngestReadDbContext>(o => o.UseNpgsql(ingestConn));

// SignalR client to publish alerts in real time
builder.Services.AddSingleton<HubConnection>(_ =>
    new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect()
        .Build());

// Background worker
builder.Services.AddHostedService<RulesWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure DB exists (MVP convenience). In production, prefer EF migrations.
using (var scope = app.Services.CreateScope())
{
    var rulesDb = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
    rulesDb.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

// --- Endpoints ---
var allowedOperators = new[] { ">", ">=", "<", "<=" };
var allowedTypes = new[] { "temperature", "co2" };

//POST /rules - add new rule
app.MapPost("/rules", async (CreateRuleRequest req, RulesDbContext db) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(req.TenantSlug))
        errors["tenantSlug"] = ["Tenant-slug måste anges."];
    if (req.DeviceId == Guid.Empty)
        errors["deviceId"] = ["DeviceId måste anges"];
    if (!allowedTypes.Contains(req.Type))
        errors["type"] = [$"Type måste vara en av: {string.Join(", ", allowedTypes)}."];
    if (!allowedOperators.Contains(req.Operator))
        errors["operator"] = [$"Operator måste vara en av: {string.Join(", ", allowedOperators)}."];

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var rule = new RuleRow
    {
        TenantSlug = req.TenantSlug,
        DeviceId = req.DeviceId,
        Type = req.Type,
        Operator = req.Operator,
        Threshold = req.Threshold
    };

    db.Rules.Add(rule);
    await db.SaveChangesAsync();
    return Results.Created($"/rules/{rule.Id}", rule);
});

//GET /rules – list rules.
app.MapGet("/rules", async (RulesDbContext db) =>
    Results.Ok(await db.Rules.OrderByDescending(r => r.CreatedAt).ToListAsync()));

//GET /alerts – list latest alerts 
app.MapGet("/alerts", async (Guid? deviceId, RulesDbContext db) =>
{
    var query = db.Alerts.AsQueryable();
    if (deviceId is Guid id)
        query = query.Where(a => a.DeviceId == id);

    var alerts = await query
        .OrderByDescending(a => a.Time)
        .Take(50)
        .ToListAsync();

    return Results.Ok(alerts);
});

app.MapGet("/", () => Results.Ok(new { service = "Rules.Engine", status = "ok" }));

// Start SignalR connection on boot
using (var scope = app.Services.CreateScope())
{
    var conn = scope.ServiceProvider.GetRequiredService<HubConnection>();
    try
    {
        await conn.StartAsync();
        app.Logger.LogInformation("Connected to Realtime Hub at {HubUrl}", hubUrl);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to connect to Realtime Hub at {HubUrl}.", hubUrl);
    }
}

app.Run();


// ===========================
// Background worker
// ===========================
public class RulesWorker : BackgroundService
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MaxMeasurementAge = TimeSpan.FromSeconds(60);
    private readonly ILogger<RulesWorker> _log;
    private readonly IServiceScopeFactory _scopefactory;
    private readonly HubConnection _hub;

    public RulesWorker(ILogger<RulesWorker> log, RulesDbContext rules, IServiceScopeFactory scopeFactory, HubConnection hub)
    {
        _log = log;
        _scopefactory = scopeFactory;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Rules engine started (poll=10s)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateRulesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled error during rule evaluation cycle.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task EvaluateRulesAsync(CancellationToken ct)
    {
        using var scope = _scopefactory.CreateScope();
        var rulesDb = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        var ingestDb = scope.ServiceProvider.GetRequiredService<IngestReadDbContext>();

        //Hämta alla aktiva regler
        var rules = await rulesDb.Rules
            .AsNoTracking()
            .Where(r => r.Enabled)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;

        foreach (var rule in rules)
        {
            //Senaste mätvärdet
            var latest = await ingestDb.Measurements
                .AsNoTracking()
                .Where(m => m.DeviceId == rule.DeviceId && m.Type == rule.Type)
                .OrderByDescending(m => m.Time)
                .FirstOrDefaultAsync(ct);

            if (latest is null)
                continue;

            if (now - latest.Time > MaxMeasurementAge)
                continue;

            if (!IsTriggered(latest.Value, rule.Operator, rule.Threshold))
                continue;
            //Cooldown
            var lastAlert = await rulesDb.Alerts
                .AsNoTracking()
                .Where(a => a.RuleId == rule.Id)
                .OrderByDescending(a => a.Time)
                .FirstOrDefaultAsync(ct);

            if (lastAlert is not null)
            {
                if (latest.Time <= lastAlert.Time)
                    continue;

                if (latest.Time - lastAlert.Time < Cooldown)
                    continue;
            }

            //Skapa och spara larmet
            var alert = new AlertRow
            {
                RuleId = rule.Id,
                TenantSlug = rule.TenantSlug,
                DeviceId = rule.DeviceId,
                Type = rule.Type,
                Value = latest.Value,
                Operator = rule.Operator,
                Threshold = rule.Threshold,
                Message = FormattableString.Invariant($"{rule.Type} {latest.Value} {rule.Operator} {rule.Threshold}"),
                Time = latest.Time
            };

            rulesDb.Alerts.Add(alert);
            await rulesDb.SaveChangesAsync(ct);

            _log.LogWarning("ALERT: device {deviceId} - {Message}", alert.DeviceId, alert.Message);

            await PublishAlertAsync(alert, ct);
        }
    }

    //Jämför värdet
    private static bool IsTriggered(double value, string op, double threshold) => op switch
    {
        ">" => value > threshold,
        ">=" => value >= threshold,
        "<" => value < threshold,
        "<=" => value <= threshold,
        _ => false
    };

    //Skicka larmet till Realtime.Hub
    private async Task PublishAlertAsync(AlertRow alert, CancellationToken ct)
    {
        try
        {
            if (_hub.State == HubConnectionState.Disconnected)
                await _hub.StartAsync(ct);

            if (_hub.State != HubConnectionState.Connected)
            {
                _log.LogWarning("Realtime.Hub not connected - alert {AlertId} saved but not published", alert.Id);
                return;
            }

            await _hub.InvokeAsync("PublishAlert", alert, ct);
            _log.LogInformation("Published alertRaised for device {DeviceId}", alert.DeviceId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to publish alert {AlertId} to Realtime.Hub", alert.Id);
        }
    }
}


// ===========================
// EF Core data model
// ===========================
public class RulesDbContext : DbContext
{
    public RulesDbContext(DbContextOptions<RulesDbContext> options) : base(options) { }
    public DbSet<RuleRow> Rules => Set<RuleRow>();
    public DbSet<AlertRow> Alerts => Set<AlertRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RuleRow>().ToTable("Rules").HasKey(r => r.Id);
        modelBuilder.Entity<AlertRow>().ToTable("Alerts").HasKey(a => a.Id);
        base.OnModelCreating(modelBuilder);
    }
}

public class IngestReadDbContext : DbContext
{
    public IngestReadDbContext(DbContextOptions<IngestReadDbContext> options) : base(options) { }
    public DbSet<MeasurementRow> Measurements => Set<MeasurementRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeasurementRow>().ToTable("Measurements").HasKey(m => m.Id);
        base.OnModelCreating(modelBuilder);
    }
}

// --- Tables ---
public class RuleRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantSlug { get; set; } = "";
    public Guid DeviceId { get; set; }                // vilken enhet regeln gäller (DeviceRegistrys GUID)
    public string Type { get; set; } = "";            // "temperature" eller "co2"
    public string Operator { get; set; } = ">";       // ">", ">=", "<" eller "<="
    public double Threshold { get; set; }             // tröskelvärdet, t.ex. 28
    public bool Enabled { get; set; } = true;         // kan stängas av utan att raderas
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AlertRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RuleId { get; set; }
    public string TenantSlug { get; set; } = "";
    public Guid DeviceId { get; set; }
    public string Type { get; set; } = "";
    public double Value { get; set; }
    public string Operator { get; set; } = "";
    public double Threshold { get; set; }
    public string Message { get; set; } = "";
    public DateTimeOffset Time { get; set; }
}

// Mirror of Ingest measurement (read-only model)
public class MeasurementRow
{
    public long Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public string Type { get; set; } = "";
    public double Value { get; set; }
}

public record CreateRuleRequest(
    string TenantSlug,
    Guid DeviceId,
    string Type,
    string Operator,
    double Threshold
);