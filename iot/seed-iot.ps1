# seed-iot.ps1
# Skapar tenant "innovia" + 10 sensorer i DeviceRegistry och kopplar varje sensor
# till en resurs i Innovia-databasen via resursens NAMN (så det funkar på alla datorer).
# Idempotent: kan köras flera gånger – det som redan finns hoppas över.
# Förutsätter: DeviceRegistry körs på 5101 och containern innovia_postgres är igång.

$registry = "http://localhost:5101"
$slug     = "innovia"

# --- Hjälpfunktion: slå upp en resurs Id i din databas utifrån namnet ---
function Get-ResourceId([string]$namePattern) {
    # SQL:en skickas via stdin (pipen |) i stället för som argument.
    # Anledning: Windows PowerShell 5.1 förstör citattecken i argument till externa
    # program, och vi behöver "Id"/"Resources" med citattecken (PascalCase-tabeller).
    $sql = 'SELECT "Id" FROM "Resources" WHERE "Name" LIKE ''' + $namePattern + ''' LIMIT 1;'

    # docker exec -i  = kör ett kommando inne i en container och skicka in stdin
    # psql -t -A      = skriv bara ut själva värdet (inga rubriker, ingen tabellram)
    $id = $sql | docker exec -i innovia_postgres psql -U innovia_user -d innovia_db -t -A

    if (-not $id) { throw "Hittade ingen resurs som matchar '$namePattern'" }
    return "$id".Trim()
}

# --- 1. Tenant: hämta om den finns, annars skapa ---
try {
    # Invoke-RestMethod kastar ett undantag vid 404 → då hamnar vi i catch
    $tenant = Invoke-RestMethod "$registry/api/tenants/by-slug/$slug"
    Write-Host "Tenant finns redan: $($tenant.id)"
}
catch {
    $body   = @{ name = "Innovia Hub"; slug = $slug } | ConvertTo-Json
    $tenant = Invoke-RestMethod "$registry/api/tenants" -Method Post -Body $body -ContentType "application/json"
    Write-Host "Skapade tenant: $($tenant.id)"
}

# --- 2. Sensorerna ---
# 'M_tesrum 1': understreck = "exakt ett valfritt tecken" i SQL LIKE.
# Vi undviker 'ö' här, eftersom åäö lätt blir fel teckenkodning
# på vägen PowerShell → docker → psql. Mönstret matchar ändå bara "Mötesrum 1".
$devices = @(
    @{ serial = "dev-101"; model = "Acme CO2-Temp";    room = "M_tesrum 1" },
    @{ serial = "dev-102"; model = "Acme CO2-Temp";    room = "M_tesrum 2" },
    @{ serial = "dev-103"; model = "Acme CO2-Temp";    room = "M_tesrum 3" },
    @{ serial = "dev-104"; model = "Acme CO2-Temp";    room = "M_tesrum 4" },
    @{ serial = "dev-105"; model = "Acme Server-Temp"; room = "AI-server" },
    @{ serial = "dev-106"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 1" },
    @{ serial = "dev-107"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 4" },
    @{ serial = "dev-108"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 7" },
    @{ serial = "dev-109"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 10" },
    @{ serial = "dev-110"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 13" },
    @{ serial = "dev-111"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 2" },
    @{ serial = "dev-112"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 3" },
    @{ serial = "dev-113"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 5" },
    @{ serial = "dev-114"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 6" },
    @{ serial = "dev-115"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 8" },
    @{ serial = "dev-116"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 9" },
    @{ serial = "dev-117"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 11" },
    @{ serial = "dev-118"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 12" },
    @{ serial = "dev-119"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 14" },
    @{ serial = "dev-120"; model = "Acme CO2-Temp";    room = "Drop-in skrivbord 15" }
)

foreach ($d in $devices) {
    # Finns enheten redan? (då ska vi inte skapa en dubblett – det finns inget unique-index!)
    try {
        Invoke-RestMethod "$registry/api/tenants/$($tenant.id)/devices/by-serial/$($d.serial)" | Out-Null
        Write-Host "Finns redan: $($d.serial)"
        continue          # hoppa till nästa enhet i loopen
    }
    catch { }             # 404 = finns inte → fortsätt nedan och skapa den

    $roomId = Get-ResourceId $d.room
    $body = @{
        model  = $d.model
        serial = $d.serial
        status = "active"
        roomId = $roomId
    } | ConvertTo-Json

    $created = Invoke-RestMethod "$registry/api/tenants/$($tenant.id)/devices" `
                 -Method Post -Body $body -ContentType "application/json"
    Write-Host "Skapade $($d.serial) -> $($d.room) (deviceId $($created.id))"
}

Write-Host "`nKlart. Tenant-ID: $($tenant.id)"