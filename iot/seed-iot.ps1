
$registry = "http://localhost:5101"
$slug     = "innovia"

function Get-ResourceId([string]$namePattern) {
    $sql = 'SELECT "Id" FROM "Resources" WHERE "Name" LIKE ''' + $namePattern + ''' LIMIT 1;'
    $id = $sql | docker exec -i innovia_postgres psql -U innovia_user -d innovia_db -t -A

    if (-not $id) { throw "Hittade ingen resurs som matchar '$namePattern'" }
    return "$id".Trim()
}

# --- 1. Tenant ---
try {
    $tenant = Invoke-RestMethod "$registry/api/tenants/by-slug/$slug"
    Write-Host "Tenant finns redan: $($tenant.id)"
}
catch {
    $body   = @{ name = "Innovia Hub"; slug = $slug } | ConvertTo-Json
    $tenant = Invoke-RestMethod "$registry/api/tenants" -Method Post -Body $body -ContentType "application/json"
    Write-Host "Skapade tenant: $($tenant.id)"
}

# --- 2. Sensorerna ---
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
    try {
        Invoke-RestMethod "$registry/api/tenants/$($tenant.id)/devices/by-serial/$($d.serial)" | Out-Null
        Write-Host "Finns redan: $($d.serial)"
        continue         
    }
    catch { }          

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