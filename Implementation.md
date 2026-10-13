Filen beskriver vilka ändringar och var ni har genomfört under projektet

Ändringar:

1. Ändrade port för innovias databas för att inte krocka med portarna i IoT-repot.
2. Realtime.Hub.csproj: Tog bort Microsoft.AspNetCore.SignalR 1.1.0. Paketet är från .NET Core 2.x, och SignalR-servern ingår redan i Microsoft.NET.Sdk.Web. Det gamla paketet drog in sårbara transitiva beroenden (byggvarningar NU190x).
3.

Ändringar kopplade till G-krav

- Minst 10 registrerade enheter i DeviceRegistry.
- Alla enheter ska ta emot mock-data via Edge.Simulator.
- Alla enheter ska kunna hämtas via DeviceRegistry API och visas i er egen portal.
- Realtidsdata ska presenteras för enheterna med hjälp av SignalR + Realtime.Hub i er egen portal.

Ändringar kopplade till VG-krav

- Bygga en enkel prototyp av Rules.Engine som:
- Låter en regel skapas för en enhet (t.ex. ”om temperaturen är högre än 28°C”).
- När mätvärden kommer in (via Ingest och Edge.Simulator), utvärderas regeln.
- Om regeln slår till, skickas en alert via Realtime.Hub som kan fångas i portalen.
  Minimikrav för VG
- Skapa regel: Skapa regeler eller via enkel endpoint (t.ex. POST /rules).
- Utvärdering: Regeln jämför senaste mätvärdet för enheten mot ett tröskelvärde.
- Alert: Om tröskeln överskrids skickas ett SignalR-event (alertRaised) till tenantens grupp i Realtime.Hub.
- Implementering i ert projekt: Plattformen kan ta emot alertRaised och skriva ut varningen i UI:t.

## Systemets mikrotjänster

Innovia Hub sensorintegration består av följande tjänster:

### DeviceRegistry.Api

Hanterar tenants, rum och enheter. Här registreras alla IoT-sensorer som används i systemet.

### Ingest.Gateway

Tar emot data från enheterna, både via HTTP och MQTT.Normaliserar och sparar mätvärden i databasen.

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
    Mosquitto	                            1883
    Redis (används inte av någon tjänst)	6379
    DeviceRegistry.Api	                    5101
    Ingest.Gateway	                        5102
    Realtime.Hub	                        5103
    Portal.Adapter	                        5104
