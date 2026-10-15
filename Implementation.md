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

### Startordning

1. Docker Desktop + docker compose -f deploy/docker-compose.yml up -d (från mappen iot/innovia-iot)
2. DeviceRegistry.Api (dotnet run)
3. Realtime.Hub (dotnet run)
4. Ingest.Gateway (dotnet run)
5. Edge.Simulator (dotnet run)
6. Innovia.Api (dotnet run --launch-profile https)
7. Frontend (npm run dev)

## Buggar/problem som fixats under projektet

1. Realtime.Hub refererade ett föråldrat SignalR-paket, (ett paket från .NET Core 2.x). Paketet var onödigt och drog in gamla, sårbara beroenden (byggvarningar NU190x).
   Lösning: Paketreferensen togs bort.

2. Mätvärden sparades inte på grund av tidszon. Edge.Simulator satte lokal tid (+02:00 i Sverige), Ingest.Gateway tog emot meddelandet via MQTT men kunde inte spara då Npgsql endast accepterar offset 0 för kolumntypen timestamp with timezone = inga mätvärden sparades.
   Lösning: En variabel lades till som normaliserar tiden till UTC som sedan används när både mätvärdet sparas och publiceras. Lades till i Ingest.Gateway istället för tex ändringar i Edge.Simulator för att man ska kunna ändra simulatorn till riktiga enheter utan att riskera krasch.

3. Alla mätvärden sparades under samma enhet. Nyckeln var inte unik så första enheten som skickade (dev-101) cachades under "innovia" och alla efterföljande enheter fick sedan dess guid från cachen, så alla mätvärden sparades som dev-101. Det funkade när det bara var en enhet som var hårdkodad och skickade, men förändringen till 10 enheter gjorde detta till en problem.
   Lösning: Cachenyckeln ändrades till $"{tenantSlug}:{deviceSerial}", så att varje enhet får en egen unik plats i cachen.

## Ändringar som gjorts för att uppfylla kraven

1. Innovias databas flyttad till port 5433 då både Innovias db och IoT-tjänsterna TimescaleDb ville använda samma port (5432).

2. Seed-skript tillagt för tenant innovia och enheterna dev-101 - dev-110. Kopplingen är att varje enhets roomId sätts till ett resourceId från Innovias Db. Id:t slås upp via resurserns namn. Skriptet kontrollerar även vad som redan finns och skapar bara det som saknas för att inte skapa dubletter om det körs flera gånger.
   Tillägg: Jag valde senare att lägga till samtliga resurser från Innovia (förutom VR-headset som inte är ett rum i sig), se 5.

3. Edge.simulatorn skickade bara data för en hårdkodad enhet (dev-101). Då krav är att alla 10 enheter ska få data minst var 10e sekund så lades en array till med de olika serials. En foreach-loop inne i while skickade ett meddelande per enhet, och de hårdkodade variablerna byttes ut mot {serials}-variabler. Intervallet är också sänkt från 10 s till 9s, eftersom ett intervall dessutom tar tiden att publicera 10 meddelande.
   Ytterligare ett tillägg här var att AI-serverrummet (dev-105) skickar högre temperaturer. Temperaturen avrundas till en decimal, och loggraden förkortades för att göra det enklare att läsa. Lokal tid är medvetet kvar eftersom det då bevisar att buggen som löstes i 2an funkar, där den lokala tiden normaliseras till utc.

4. Backend: Ny feature (sensor) och endpoint GET /sensors i Innovia.Api (Features/Sensors). Hämtar listan med alla 10 sensorer och visar vilket rum varje sensor sitter i, tex dev-101 - Mötesrum 1. Endpointen används av sensorsidan i frontend.Typad Httpklient som samlar all kommunikation med DeviceRegistry.Api (DeviceRegistryClient). Om DeviceRegistry.Api ändrar sitt format behöver bara backend anpassas, inte frontend.
   Handlern slår upp tenant via slug (konfigurerad i appsettings.json), hämtar enheterna och kopplar varje RoomId till Resource.Name i en enda databasfråga (undviker N+1).
   Egna Response-records, så att frontend inte är beroende av DeviceRegistry.Apis format.
   Felhantering: Om DeviceRegistry.Api är avstängd eller inte svarar inom 5 sekunder får användaren ett tydligt meddelande ("Sensortjänsten är inte tillgänglig just nu") med statuskod 503, i stället för att appen kraschar. För detta lades en ny feltyp till i projektets felhantering.En felkonfigurerad tenant ger 500.
   Behörighet: AdminOnly.
   Testat: 200 med 10 sensorer och rumsnamn vid inloggning som admin, 401 utan inloggning, 503 med DeviceRegistry.Api avstängd.

5. Utökning till 20 sensorer: Sensorer finns i alla mötesrum (4), vid alla drop-in-skrivbord (15) och i AI-serverrummet (1). VR-headset fick inga sensorer, eftersom det är en enhet, inte ett rum i sig. Ändringen krävde bara två justeringar (seed-skriptet och antalet i Edge.Simulator).

6. Lade till en ny sida i frontend som där samtliga resurser (förutom VR-headset) syns som kort med sensorinformation. Korten är grupperade under rubrikerna för de olika resurstyperna och innehåller rummets namn, temperatur, C02-halt, samt sensors serienummer och modell. Endast admin kan se sidan.

7. Realtidsdata: Ändrade CORS på RealtimeHub att tillåta min frontend, skapade anslutning till RealtimeHub från frontend, en hook som tar emot och sparar värden samt visar ut livedata-värden på sensorkorten i frontend. Om DeviceRegistry.Api är nere visas backendens felmeddelande ("Sensortjänsten är inte tillgänglig just nu") i en röd ruta på sidan.Datan uppdateras live var 9:e sekund, och en grön live-indikator visar att anslutningen fungerar. Korten har även uppdaterats med klockslaget för senaste mätningen.
   Obs säkerhetsbrist: Realtime.Hub har ingen inloggning, så vem som helst kan både lyssna på anslutningen och skicka falska värden.
   Möjliga lösningar:

- En hemlig nyckel som bara Ingest.Gateway får använda för att skicka mätvärden.
- En kortlivad "biljett" från Innovia.Api som krävs för att lyssna.
- Låta Innovia.Api ta emot mätvärdena och skicka dem vidare via en egen, inloggningsskyddad hubb. Då kan Realtime.Hub vara helt dold.

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
