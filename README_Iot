## Översikt

Innovia Hubs plattform har kopplats ihop med IoT-tjänster, där simulerade sensorer (temperatur och CO2) skickar mätvärden. Integrationen innebär att 20 simulerade sensorer i mötesrum, vid skrivbord och i AI-serverrummet nu skickar mätvärden var 9:e sekund, som admin ser live på sidan Sensorer i färdiga kort där även rumsnamn, modell, och tid för senaste mätvärdet visas.

## Systemets mikrotjänster

Innovia Hub sensorintegration består av följande tjänster:

### DeviceRegistry.Api

Hanterar tenants, rum och enheter. Här registreras alla IoT-sensorer som används i systemet.

### Ingest.Gateway

Tar emot data från enheterna, både via HTTP och MQTT. Normaliserar och sparar mätvärden i databasen.

### Realtime.Hub

SignalR-hubb som skickar vidare senaste mätvärdena i realtid till anslutna klienter.

### Portal.Adapter

Ger externa system (t.ex. er portal) möjlighet att hämta data via enklare REST-API.

### Rules.Engine

Worker service som kan hantera regler och generera alerts baserat på inkommande data.

### Edge.Simulator

En konsolapp som simulerar IoT-enheter och skickar data via MQTT. Används för att testa utan fysisk hårdvara.

## Portlista

#### Tjänst Port

    Min Postgres (innovia_postgres)	        5433
    IoT TimescaleDB (deploy-db-1)	        5432
    Mosquitto	                             1883
    Redis (används inte av någon tjänst)	  6379
    DeviceRegistry.Api	                    5101
    Ingest.Gateway	                       5102
    Realtime.Hub	                          5103
    Portal.Adapter	                       5104
    Rules.Engine                            5105
    Innovia.Api                             7229
    Frontend                                5173

## Så kör du projektet

### Förutsättningar

- Docker Desktop, .NET 8 SDK, .NET 10 SDK, Node.js
- OpenAI-nyckel för Innovia.Api (krävs för att API:t ska starta, se README_Nova.md):
  `cd Innovia.Api` → `dotnet user-secrets set "OpenAI:ApiKey" "sk-..."`

### Första gången (i denna ordning)

| #   | Vad                                                      | Kommando (från repo-roten)                                                 |
| --- | -------------------------------------------------------- | -------------------------------------------------------------------------- |
| 1   | Innovias databas                                         | `docker compose up -d`                                                     |
| 2   | IoT-infrastruktur (TimescaleDB, Mosquitto, Redis)        | `cd iot/innovia-iot` → `docker compose -f deploy/docker-compose.yml up -d` |
| 3   | Innovia.Api – skapar och fyller databasen med resurser   | `cd Innovia.Api` → `dotnet run --launch-profile https`                     |
| 4   | DeviceRegistry.Api                                       | `cd iot/innovia-iot/src/DeviceRegistry.Api` → `dotnet run`                 |
| 5   | Skapa tenant + 20 sensorer (kräver att 3 och 4 körs)     | `cd iot` → `powershell -ExecutionPolicy Bypass -File .\seed-iot.ps1`       |
| 6   | Realtime.Hub                                             | `cd iot/innovia-iot/src/Realtime.Hub` → `dotnet run`                       |
| 7   | Ingest.Gateway (efter Realtime.Hub, annars kraschar den) | `cd iot/innovia-iot/src/Ingest.Gateway` → `dotnet run`                     |
| 8   | Edge.Simulator                                           | `cd iot/innovia-iot/src/Edge.Simulator` → `dotnet run`                     |
| 9   | Rules.Engine (regler och larm, VG)                       | `cd iot/innovia-iot/src/Rules.Engine` → `dotnet run`                       |
| 10  | Frontend                                                 | `cd frontend` → `npm install` → `npm run dev`                              |

Logga in på http://localhost:5173 som admin (`admin@admin.com` / `Password123!`) och välj **Sensorer** i menyn.

Skapa en larmregel i Swagger (http://localhost:5105/swagger) med `POST /rules`.
Enhetens id (GUID) hämtas från DeviceRegistry.Api.

### Följande gånger

Samma ordning, men steg 5 (seed-skriptet) behövs inte. Det kan dock köras igen utan risk, eftersom det bara skapar det som saknas.
