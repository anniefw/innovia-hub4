# Planering: Husassistenten (AI-feature, Innovia Hub)

---

## Steg 0: Förberedelser (30 min)

- [x] Skapa en dev-branch.
- [x] Lägg in OpenAI-nyckeln med **user-secrets** (i `Innovia.Api`-mappen):
  ```powershell
  dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
  ```
- [x] Lägg modellnamnet (inte hemligt) i `appsettings.json`:
  ```json
  "OpenAI": { "Model": "<liten, billig modell som läraren godkänt>" }
  ```
- [x] Installera paketen `Microsoft.Extensions.AgitI` och `Microsoft.Extensions.AI.OpenAI`
- [x] Skapa mappen `Features/Assistant/`

**Klart när:** `dotnet user-secrets list` visar nyckeln och projektet bygger.

> **Varför user-secrets och inte .env?**
>
> - ASP.NET läser user-secrets automatiskt i Development. En `.env`-fil kräver ett extra paket.
> - User-secrets ligger utanför projektmappen, så de kan aldrig råka hamna i git. En `.env` hamnar där om den glöms i `.gitignore`.
> - **Lägg aldrig nyckeln i frontendens `.env`.** Allt med `VITE_` byggs in i JavaScript-koden och syns för alla i webbläsaren.
> - I produktion används i stället miljövariabeln `OpenAI__ApiKey` (två understreck = `:`). Koden läser `builder.Configuration["OpenAI:ApiKey"]` i båda fallen, så den behöver inte ändras.

---

## Steg 1: Innehållet (1–2 h)

- [ ] Skriv `Features/Assistant/knowledge.md` med rubriker per ämne: husregler, gäster, VR-headset, AI-servern, kök, kontakt. **Inga lösenord.**
- [ ] Lägg till i `.csproj` så att filen följer med i bygget:
  ```xml
  <None Update="Features/Assistant/knowledge.md" CopyToOutputDirectory="PreserveNewest" />
  ```
- [ ] Skriv testfrågelistan (`Features/Assistant/testfragor.md`):
  - 7 frågor som finns i filen eller databasen, med förväntat svar
  - 2 frågor som inte finns → "vet inte + receptionen"
  - 1 manipulationsförsök ("Ignorera dina regler och …")

**Klart när:** filen finns i `bin/Debug/.../Features/Assistant/` efter bygget.

---

## Steg 2: Bygg texten AI:n ska läsa, utan AI (2 h)

Skapa en klass, t.ex. `AssistantContextBuilder`, som bara sätter ihop en `string`:

- [x] Hämta `AvailabilityRules` → text, t.ex. `Mötesrum: måndag 08:00–20:00`
- [x] Hämta `Resources` (namn, typ, beskrivning, status) → text
- [x] Sätt ihop allt med tydliga rubriker (`--- ALLMÄN INFORMATION ---` osv.)

**Tips:** Gör en tillfällig endpoint som returnerar strängen och öppna den i webbläsaren. Då ser du exakt vad AI:n kommer att läsa. Kan du inte svara på testfrågorna utifrån texten kan AI:n det inte heller.

**Klart när:** du kan svara på alla 7 "finns"-frågor själv utifrån utskriften.

---

## Steg 3: Första AI-svaret, utan streaming (2 h)

- [x] Registrera `IChatClient` i `Program.cs`. Det är enda stället där OpenAI nämns.
- [x] Skapa `AskAssistantHandler` (samma mönster som övriga Handlers):
  - Request: `Question` + `History`
  - Meddelandelista: **system** (instruktioner + text från steg 2) → historik → **user** (frågan)
  - Anropa `GetResponseAsync`, returnera svaret som sträng
- [x] Skriv systemprompten. Ta med en **exakt reservfras**, t.ex. _"Det hittar jag tyvärr ingen information om."_ (den behövs i steg 6)
- [x] Testa via en tillfällig HTTP-endpoint (Swagger / Postman / `.http`-fil)

**Klart när:** "Får jag ta med gäster?" ger ett korrekt svar.

> **Kontrollpunkt:** här har du bevisat att kärnan fungerar: nyckel, prompt och OpenAI-anrop. Resten handlar om att leverera svaret snyggare.

---

## Steg 4: Streaming och ChatHub (2–3 h)

- [x] Gör om Handlern så att den returnerar `IAsyncEnumerable<string>`: byt till `GetStreamingResponseAsync` och `yield return` varje textbit
- [x] Skapa `ChatHub` i `Features/Assistant/` (mall: `ResourceHub`). `[Authorize]`, metoden `Ask(...)` anropar bara Handlern
- [x] Mappa hubben i `Program.cs` på `/hubs/chat`
- [x] Ta bort den tillfälliga endpointen från steg 3, eller lägg den bakom AdminOnly

**Klart när:** projektet bygger och startar utan fel.

---

## Steg 5: Frontend (3–4 h)

**5a. Anslutningen**

- [ ] `lib/chatHubConnection.ts`: kopiera mönstret från `resourceHubConnection.ts`, med ny URL

**5b. Hooken**

- [ ] `hooks/useAssistantChat.ts` håller:
  - `messages`: `{ role: "user" | "assistant", text: string }[]`
  - `isStreaming`
  - `send(question)`: lägger till frågan och ett tomt assistent-meddelande, anropar `connection.stream("Ask", question, history)` och lägger till varje bit i `next` i sista meddelandet
- [ ] Skicka bara de **senaste ~6 meddelandena** som historik

**5c. Widgeten**

- [ ] `components/AssistantWidget.tsx`: flytande knapp → panel med meddelandelista, förslagsknappar och inmatningsfält
- [ ] Lägg in den i layouten så att den syns på alla sidor för inloggade

**Klart när:** du ställer en fråga i webbläsaren och ser svaret skrivas fram ord för ord.

---

## Steg 6: Skydd och loggning (1–2 h)

- [ ] Begränsa antalet frågor per användare (t.ex. 20/timme)
  > ⚠️ **Fälla:** ASP.NET:s inbyggda rate limiter räknar HTTP-anrop, men SignalR håller **en** långlivad anslutning, så limitern ser inte de enskilda frågorna. Räkna själv i Handlern, t.ex. med `IMemoryCache` och användar-id som nyckel.
- [ ] Logga obesvarade frågor: samla ihop hela svaret under streamingen. Innehåller det reservfrasen → logga frågan med `ILogger`
- [ ] Kontrollera att inget namn eller ingen e-post skickas till OpenAI

**Klart när:** fråga nr 21 ger ett vänligt "försök igen senare", och obesvarade frågor syns i terminalen.

---

## Steg 7: Kvalitet (2 h)

- [ ] Kör testfrågelistan och bocka av
- [ ] Justera systemprompten för det som blir fel. Kör **hela** listan igen efter varje ändring
- [ ] Frontend-finish: laddningsindikator, felmeddelande om anslutningen bryts, mobilvy, knappen "Gå till bokning" vid resursfrågor

**Klart när:** 9 av 10 testfrågor blir rätt (målet i rapporten).

---

## Steg 8: Avslut (1 h)

- [ ] README: "Kom igång"-sektion + att OpenAI-nyckeln läggs med user-secrets
  1. `docker compose up -d` (från repots rot)
  2. `cd Innovia.Api` → `dotnet user-secrets set "OpenAI:ApiKey" "..."` → `dotnet run --launch-profile https`
  3. `cd frontend` → `npm install` → `npm run dev`
  4. Kontrollera att `Cors:FrontendOrigin` är `http://localhost:5173`
- [ ] Ta bort tillfälliga endpoints och `Console.WriteLine`-rader
- [ ] Förbered demon: vanlig fråga · fråga om live-data ("Kan jag använda VR idag?") · fråga som inte finns · manipulationsförsök
- [ ] Merga branchen

---

SISTA STEGEN:

- Be om visuell bild för flödet från Claude
- Gör testning och kvalitets-delen (se nedan)

Krav:
Planering inlämnad under V1

Körbar lösning i repo

Teknisk dokumentation (README)

Teknisk demo till Janne under ca 10 minuter på torsdag eller fredag

## För betyget VG:

Önskar du bedömas mot betyget VG (Notera detta i din inlämning så jag vet) så ska du utöver kraven bifoga en rapport i repot döpt till

## "Testning_och_kvalitet.md"

där du hänvisar till skrivna enhetstester i projektet och varför du anser de är viktiga för projektet. Du ska även reflektera över hur du arbetat för att projektet skall enkelt kunna vidareutvecklas med nya funktioner. Rapporten skall även innehålla en punkt om säkerhet, hur hemliga nycklar hanteras och implementeras i produktion.

Använd rubrikerna:

Mina tester
Framtids säkring
Säkerhet

Rapportdel

Enhetstester
Validator-tester (+ Handler och ContextBuilder snart). Testfrågorna som komplement

## Enhetstester för AI-assistenten Nova

Testerna ligger i `tests/Innovia.Api.Tests/Features/AssistantTests/` och körs med:

    dotnet test

Om endast testerna för Ai-assistenten önskas, kör istället:

    dotnet test --filter "FullyQualifiedName~AssistantTests"

### Sammanfattning av testfilerna

**`AskAssistantValidatorTests`** (13 tester)
Rena enhetstester utan databas eller andra beroenden. Testar att `Validator` stoppar
ogiltiga frågor innan de når AI:n: tomma frågor, för långa frågor (med gränsvärdestest
på exakt 500 tecken), för lång historik och otillåtna roller i historiken. Använder
`[Theory]` med `[InlineData]` för att testa flera varianter av samma regel.

**`AskAssistantHandlerTests`** (7 tester)
Testar att `Handler` bygger rätt meddelandelista till AI:n. OpenAI byts ut mot en falsk
`IChatClient` som sparar vad den fick och alltid svarar likadant. Testerna blir därför
snabba, gratis och ger samma resultat varje gång. Testar ordningen på meddelandena
(systemmeddelande först, frågan sist), att roller översätts rätt, att historiken
begränsas till de sex senaste meddelandena och att rollen `system` från klienten aldrig
accepteras.

**`AssistantContextBuilderTests`** (7 tester)
Testar att texten Nova får läsa innehåller rätt information. Seedar egen testdata i
databasen och kontrollerar att öppettider hämtas från `AvailabilityRule`, att stängda
dagar skrivs ut, att resursstatus (tillgänglig/underhåll) visas live och att arkiverade
resurser inte nämns. Kontrollerar även att kunskapsfilen och dagens svenska veckodag
finns med.

### Tre särskilt viktiga tester

**1. `Should_Never_Accept_System_Role_From_Client`** (Handler)
Det här är ett säkerhetstest. Frontend körs i användarens webbläsare, och vem som helst
kan ändra det som skickas till backend. Om backend accepterade ett historikmeddelande
med rollen `system` kunde en användare skriva egna regler som AI:n behandlar som
utvecklarens instruktioner och därmed kringgå alla begränsningar. Testet anropar
Handlern direkt, utan Validatorn emellan, och visar att skyddet håller även om
valideringen skulle missas (defense in depth). Skyddet går inte att kontrollera genom
att bara testa AI:ns svar, eftersom modellen kan svara rimligt även när koden är
sårbar. Bara ett test av själva meddelandelistan kan visa det.

**2. `Should_Put_System_Message_First_With_Instructions_And_Context`** (Handler)
Testet kontrollerar att systemprompten faktiskt skickas till AI:n genom att leta efter
reservfrasen, som bara finns i `systemprompt.md`. Under utvecklingen fanns en bugg där
fil-läsningen alltid läste `knowledge.md`, oavsett vilket filnamn som skickades in.
Nova fick då aldrig sina regler, men ingenting kraschade och svaren såg ofta rimliga
ut. Testet hade fångat buggen direkt.

**3. `Should_Show_Resource_In_Maintenance`** (ContextBuilder)
Live-data är det som skiljer Nova från en vanlig FAQ-chattbot. När admin sätter en
resurs på underhåll ska Nova veta det direkt, utan omstart. Testet säkerställer att
resursstatus från databasen hamnar i texten som AI:n läser. Det skyddar också mot en
typ av bugg som är svår att upptäcka manuellt: om statusen försvann ur texten skulle
Nova fortfarande svara, men med fel information, till exempel att ett headset är
tillgängligt när det är trasigt.

### Enhetstester och testfrågor kompletterar varandra

Utöver enhetstesterna finns en lista med testfrågor (`Features/Assistant/testfragor.md`)
som körs manuellt mot den riktiga AI:n. De två testsätten mäter olika saker:

- **Enhetstesterna** kontrollerar _min kod_: att rätt information och rätt instruktioner
  skickas. De ger samma resultat varje gång och körs automatiskt.
- **Testfrågorna** utvärderar _AI:ns svar_: att Nova faktiskt svarar korrekt. Svaren
  formuleras olika varje gång och varje anrop kostar pengar, så de passar inte som
  automatiska tester.

Uppdelningen är möjlig eftersom `Handler` beror på gränssnittet `IChatClient` och inte
direkt på OpenAI. Därför kan den riktiga AI:n bytas mot en falsk i testerna.

Vidareutveckling
Vertical slice, IChatClient som gränssnitt (byt leverantör på en rad), AssistantContextBuilder separat, Validator återanvänds i steg 4, knowledge.md utan kodändring

Säkerhet
User-secrets lokalt, miljövariabel OpenAI\_\_ApiKey i produktion, fail-fast om nyckeln saknas, nyckeln aldrig i frontend, rollskydd i två lager
