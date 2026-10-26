# Implementation

Översikt och instruktioner för att starta projektet finns i [README_Iot.md](README_Iot.md).

## Ändringar som gjorts för att uppfylla kraven

1. Innovias databas flyttad till port 5433 då både Innovias db och IoT-tjänsterna TimescaleDb ville använda samma port (5432).

2. Seed-skript tillagt för tenant innovia och enheterna dev-101 - dev-120 (ursprungligen 10 enheter, utökade till 20 under projektets gång). Kopplingen är att varje enhets roomId sätts till ett resourceId från Innovias Db. Id:t slås upp via resurserns namn. Skriptet kontrollerar även vad som redan finns och skapar bara det som saknas för att inte skapa dubletter om det körs flera gånger.

3. Edge.simulatorn skickade bara data för en hårdkodad enhet (dev-101). Då krav är att alla 10 enheter ska få data minst var 10e sekund så lades en array till med de olika serials. En foreach-loop inne i while skickade ett meddelande per enhet, och de hårdkodade variablerna byttes ut mot {serials}-variabler. Intervallet är också sänkt från 10 s till 9s, eftersom ett intervall dessutom tar tiden att publicera 10 meddelande.
   Ytterligare ett tillägg här var att AI-serverrummet (dev-105) skickar högre temperaturer. Temperaturen avrundas till en decimal, och loggraden förkortades för att göra det enklare att läsa. Lokal tid är medvetet kvar eftersom det då bevisar att buggen som löstes i 2an funkar, där den lokala tiden normaliseras till utc.

4. Backend: Ny feature (sensor) och endpoint GET /sensors i Innovia.Api (Features/Sensors). Hämtar listan med alla sensorer och visar vilket rum varje sensor sitter i, tex dev-101 - Mötesrum 1, typ "mötesrum". Endpointen används av sensorsidan i frontend.Typad Httpklient som samlar all kommunikation med DeviceRegistry.Api (DeviceRegistryClient). Om DeviceRegistry.Api ändrar sitt format behöver bara backend anpassas, inte frontend.
   Handlern slår upp tenant via slug (konfigurerad i appsettings.json), hämtar enheterna och kopplar varje RoomId till Resource.Name i en enda databasfråga (undviker N+1).
   Egna Response-records, så att frontend inte är beroende av DeviceRegistry.Apis format.
   Felhantering: Om DeviceRegistry.Api är avstängd eller inte svarar inom 5 sekunder får användaren ett tydligt meddelande ("Sensortjänsten är inte tillgänglig just nu") med statuskod 503, i stället för att appen kraschar. För detta lades en ny feltyp till i projektets felhantering. En felkonfigurerad tenant ger 500.
   Behörighet: AdminOnly.
   Testat: 200 med 10 sensorer och rumsnamn vid inloggning som admin, 401 utan inloggning, 503 med DeviceRegistry.Api avstängd.

5. Utökning till 20 sensorer: Sensorer finns i alla mötesrum (4), vid alla drop-in-skrivbord (15) och i AI-serverrummet (1). VR-headset fick inga sensorer, eftersom det är enheter, inte rum i sig. Ändringen krävde bara två justeringar (seed-skriptet och antalet i Edge.Simulator).

6. Lade till en ny sida i frontend där samtliga resurser (förutom VR-headset) syns som kort med sensorinformation. Korten är grupperade under rubrikerna för de olika resurstyperna och innehåller rummets namn, temperatur, CO2-halt, samt sensors serienummer och modell. Endast admin kan se sidan.

7. Realtidsdata: Ändrade CORS på RealtimeHub att tillåta min frontend, skapade anslutning till RealtimeHub från frontend, en hook som tar emot och sparar värden samt visar ut livedata-värden på sensorkorten i frontend. Om DeviceRegistry.Api är nere visas backendens felmeddelande ("Sensortjänsten är inte tillgänglig just nu") i en röd ruta på sidan.Datan uppdateras live var 9:e sekund, och en grön live-indikator visar att anslutningen fungerar. Korten har även uppdaterats med klockslaget för senaste mätningen.
   Obs säkerhetsbrist: Realtime.Hub har ingen inloggning, så vem som helst kan både lyssna på anslutningen och skicka falska värden.

8. Tillägg gjordes till den befintliga Rules.Engines som hade flera TODO-kommentarer för att kunna skapa regler, och lista regler och alerts.

- Regelmodellen fylldes i med enhet, mättyp (temperatur/CO₂), jämförelse och tröskelvärde. Regeln sparar tenantens slug.
- Larmmodellen sparar en kopia av regelns villkor, så att gamla larm går att förstå även om regeln ändras.
- TODO-endpointes fylldes i för att skapa, lista regler och lista alerts (50 senaste).
- Felaktig typ eller operator ger 400 med en förklaring, i stället för en regel som tyst aldrig slår till.
- Eget request-objekt bygges så att klienten inte kan sätta regelns ID själv.
- Rules.Engine fick appsettings.json (port 5105) och Swagger för att kunna testa endpoints.

9. Reglerna utvärderas automatiskt, jämförs var 10e sekund men enhetens senaste mätvärde. Om regeln slår till sparas ett larm. Samma regel larmar högst 1 ggr per minut, så inte admin blir överbelastad av för många larm. Samma mätvärde ger inte två larm, och ganle mätvärdet ignoreras.
   En tillagd regel om att temperaturen i dev-105 (AI-serverrummet) inte får gå över 28 grader är testad och ger larm ca 1 ggr/minuten (testad via info i konsolen, via GET/alerts i Swagger och direkt på UI i frontend)

10. Larm skickas i realtid via Realtime.Hub (alertRaised). RealtimeHub fick en ny metod (PublishAlert) som skickar eventet alertRaised till tenantgruppen (innovia). Rules.Engine skickar larmet direkt efter att det sparats (sparas först ifall utskicket misslyckas). Testat för att se alerten både i konsolen för webbläsaren och i konsolen för Rules.Engine.

11. Larm visas på Sensor-sidan för admin via en alertBanner, samt att kortet får en röd ram. Admin kan stänga larmet med ett kryss och högst fem larm visas samtidigt för att inte överbelasta admin. Larmen tas emot över samma signalR-anslutning men hanteras i egen hook.

## Buggar/problem som fixats under projektet

1. Realtime.Hub refererade ett föråldrat SignalR-paket, (ett paket från .NET Core 2.x). Paketet var onödigt och drog in gamla, sårbara beroenden (byggvarningar NU190x).
   Lösning: Paketreferensen togs bort.

2. Mätvärden sparades inte på grund av tidszon. Edge.Simulator satte lokal tid (+02:00 i Sverige), Ingest.Gateway tog emot meddelandet via MQTT men kunde inte spara då Npgsql endast accepterar offset 0 för kolumntypen timestamp with timezone = inga mätvärden sparades.
   Lösning: En variabel lades till som normaliserar tiden till UTC som sedan används när både mätvärdet sparas och publiceras. Lades till i Ingest.Gateway istället för tex ändringar i Edge.Simulator för att man ska kunna ändra simulatorn till riktiga enheter utan att riskera krasch.

3. Alla mätvärden sparades under samma enhet. Nyckeln var inte unik så första enheten som skickade (dev-101) cachades under "innovia" och alla efterföljande enheter fick sedan dess guid från cachen, så alla mätvärden sparades som dev-101. Det funkade när det bara var en enhet som var hårdkodad och skickade, men förändringen till flera enheter gjorde detta till ett problem.
   Lösning: Cachenyckeln ändrades till $"{tenantSlug}:{deviceSerial}", så att varje enhet får en egen unik plats i cachen.

4. Rules.Engines bakgrundstjänst var registrerad som singleton som lever lika länge som appen, men tog emot databasobjekt (DbContext) som är scoped, alltså tänkta att bara leva under ett anrop. Eftersom bakgrundstjänsten aldrig skapas om, användes samma databasobjekt i alla varv under hela appens livstid. Det kan ge gammal data, växande minnesanvändning och ett trasigt läge efter ett databasfel.
   Lösning: Bakgrundstjänsten tar istället emot IServiceScopeFactory och skapar ett nytt scope, med nya databasobjekt, i varje varv.
