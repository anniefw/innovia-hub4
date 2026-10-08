# README för husassistenten NOVA (Ai-implementering V2)

Det här är en kompletterande ReadMe för Ai-tjänsten Nova som är en husassistent med syfte att svara på frågor hos inloggade medlemmar. Svaren skickas i realtid och visas som en chattwidget.

## Om Nova

Nova svarar utifrån två olika informationskanaler.

1. Knowledge.md som är en fil skapad med "husets information". Denna kan man lätt utöka med ytterligare info.
2. Live-data ur databasen, som rör frågor kring tex dagens datum/tid, bokningsbara tider per resurs och resurstyp, och den aktuella status för de olika resurser (tex online, offline, underhåll eller arkiverad).

## Innehåll

- [Funktioner](#funktioner)
- [Kom igång med Nova](#kom-igång-med-nova)
- [Konfiguration](#konfiguration)
- [Filer och ansvar](#filer-och-ansvar)
- [Så vet Nova saker](#så-vet-nova-saker)
- [Uppdatera Novas kunskap](#uppdatera-novas-kunskap)
- [Tester](#tester)
- [Felsökning](#felsökning)
- [Säkerhet i korthet](#säkerhet-i-korthet)

---

## Funktioner

| Funktion                                | Hur                                                                                                              |
| --------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Chattwidget på alla sidor för inloggade | `AssistantForLoggedIn` i `App.tsx`. En flytande boll öppnar panelen (helskärm på mobil).                         |
| Strömmade svar                          | SignalR-hubben `/hubs/chat` returnerar `IAsyncEnumerable<string>`. Texten växer fram medan OpenAI skriver.       |
| Följdfrågor                             | De 6 senaste meddelandena skickas med som historik (tex földjfrågan "Hur många då?" fungerar, se testfragor.md). |
| Live-data                               | Öppettider och resursstatus läses ur PostgreSQL vid varje fråga.                                                 |
| Reservsvar och loggning                 | "Det hittar jag tyvärr ingen information om" + hänvisning till receptionen. Frågan loggas med `ILogger`.         |
| Skydd mot manipulation                  | Systemprompten, validering av roller i historiken och att bara backend kan skapa systemmeddelandet.              |
| Begränsning                             | Max 20 frågor per användare och timme (`AssistantRateLimiter`).                                                  |
| "Gå till bokning →"                     | Visas under svar som nämner en bokningsbar resurs.                                                               |
| Anslutningsstatus                       | Gul banner vid återanslutning, röd när anslutningen är bruten.                                                   |
| Stopp-knapp                             | Avbryter strömmen. `CancellationToken` stoppar även anropet till OpenAI.                                         |

---

## Kom igång med Nova

Följ först **Kom igång lokalt** i [README.md](README.md). Nova kräver sedan ett steg till: en OpenAI-nyckel.

> ⚠️ Utan nyckel **startar inte API:et**.

### Alternativ B: native (rekommenderas)

```powershell
cd Innovia.Api
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."   # en gång per dator
dotnet user-secrets list                            # kontrollera
dotnet run
```

Nyckeln sparas utanför repot (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` på Windows) och kan alltså inte råka committas.

### Alternativ A: allt i Docker

User-secrets når inte in i containern. Sätt därför nyckeln som miljövariabel i en `.env`-fil bredvid `docker-compose.dev.yaml` (`.env` ignoreras av git):

```env
OPENAI_API_KEY=sk-...
```

```bash
docker compose -f docker-compose.dev.yaml up
```

### Testa

1. Logga in i frontend (`http://localhost:5173`).
2. Klicka på Nova-bollen nere till höger.
3. Prova förslagsknapparna, skriv en egen fråga eller pröva frågor från [testfragor.md](Innovia.Api/Features/Assistant/testfragor.md).

---

## Konfiguration

| Nyckel                          | Var                                           | Standard                | Beskrivning                                                  |
| ------------------------------- | --------------------------------------------- | ----------------------- | ------------------------------------------------------------ |
| `OpenAI:ApiKey`                 | user-secrets / miljövariabel `OpenAI__ApiKey` | – (krävs)               | **Hemlig.** Ligger aldrig i appsettings, git eller frontend. |
| `OpenAI:Model`                  | `appsettings.json`                            | `gpt-4.1`               | Vilken modell som används. Inte hemligt.                     |
| `Assistant:MaxQuestionsPerHour` | `appsettings.json`                            | `20`                    | Frågor per användare och timme.                              |
| `Cors:FrontendOrigin`           | `appsettings.json`                            | `http://localhost:5173` | Måste matcha frontendens adress, annars blockeras SignalR.   |

I produktion sätts värdena som miljövariabler. Dubbla understreck motsvarar kolon: `OpenAI__ApiKey`, `OpenAI__Model`, `Assistant__MaxQuestionsPerHour`.

---

## Filer och ansvar

### Backend – `Innovia.Api/Features/Assistant/` (vertical slice)

| Fil                             | Ansvar                                                                                                                                                                |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AssistantServiceExtensions.cs` | `AddAssistantFeature()` registrerar allt (IChatClient, Handler, Validator, ContextBuilder, RateLimiter). `MapAssistantEndpoints()` mappar hubben och admin-endpoints. |
| `ChatHub.cs`                    | SignalR-hub (`[Authorize]`). `Ask()` validerar, kollar rate limit och strömmar svaret från Handlern.                                                                  |
| `AskAssistant/Request.cs`       | `Request(Question, History)` och `ChatHistory(Role, Text)`.                                                                                                           |
| `AskAssistant/Validator.cs`     | Frågan 1–500 tecken, max 20 historikmeddelanden, roller bara `user`/`assistant`.                                                                                      |
| `AskAssistant/Handler.cs`       | Bygger meddelandelistan (`BuildMessagesAsync`), anropar `IChatClient`, strömmar (`StreamAsync`) eller returnerar helt svar (`HandleAsync`). Loggar obesvarade frågor. |
| `AskAssistant/Endpoint.cs`      | `POST /assistant/ask`, ett icke-strömmat svar för felsökning i Scalar (AdminOnly).                                                                                    |
| `AssistantContextBuilder.cs`    | Sätter ihop texten Nova läser: aktuell tid, `knowledge.md`, öppettider och resursstatus.                                                                              |
| `AssistantRateLimiter.cs`       | Räknar frågor per användare i ett fönster på en timme (`IMemoryCache` + `TimeProvider`).                                                                              |
| `systemprompt.md`               | Novas regler och personlighet.                                                                                                                                        |
| `knowledge.md`                  | Husets fakta. Inga lösenord.                                                                                                                                          |
| `testfragor.md`                 | Manuell testlista med resultat.                                                                                                                                       |

### Frontend – `frontend/src/`

| Fil                              | Ansvar                                                                                                                 |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `lib/chatHubConnection.ts`       | En delad SignalR-anslutning: token-refresh före start, automatisk återanslutning, `streamAsk()` och anslutningsstatus. |
| `hooks/useAssistantChat.ts`      | Chattens tillstånd: `messages`, `isStreaming`, `error`, `connectionStatus`, `send`, `stop` och `reset`.                |
| `components/AssistantWidget.tsx` | Gränssnittet: boll, panel, förslag, meddelanden, skrivprickar, bannrar och bokningslänk.                               |
| `auth/AuthContext.tsx`           | Stänger chattanslutningen vid utloggning (`stopChatHubConnection`).                                                    |

---

## Så vet Nova saker

Nova använder **context stuffing**: vid varje fråga skickas all relevant information med i ett systemmeddelande. Meddelandelistan till OpenAI ser ut så här:

```text
[system]    systemprompt.md
            --- AKTUELL TID ---                     (svensk tid och veckodag)
            --- ALLMÄN INFORMATION ---              (knowledge.md)
            --- BOKNINGSBARA TIDER PER RESURSTYP --- (AvailabilityRules)
            --- RESURSER OCH AKTUELL STATUS ---     (Resources, utan arkiverade)
[user/assistant]  … de 6 senaste meddelandena (historik)
[user]      frågan
```

Kunskapen är liten (några få sidor), så det här räcker i nuläget. Ingen vektordatabas behövs. Växer kunskapen kraftigt är nästa steg RAG, alltså att bara hämta de stycken som är relevanta för frågan.

---

## Uppdatera Novas kunskap

- **Fakta** (husregler, kök med mera): redigera `knowledge.md`. Ingen kod behöver ändras. Filen kopieras till `bin` vid bygget (se `ItemGroup` i `Innovia.Api.csproj`).
- **Beteende** (ton, regler, reservfras): redigera `systemprompt.md`.
  - ⚠️ Ändrar du reservfrasen måste `Handler.FallBackPhrase` matcha, annars slutar loggningen av obesvarade frågor att fungera. Testet `Should_Use_Same_Message_List_When_Streaming` blir rött om de inte matchar.
- **Öppettider och resurser**: ändras i databasen. Nova ser ändringen vid nästa fråga.
- Kör [testfragor.md](Innovia.Api/Features/Assistant/testfragor.md) efter varje ändring.

---

## Tester

38 enhetstester för Nova ligger i `tests/Innovia.Api.Tests/Features/AssistantTests/`:

| Fil                               | Antal | Testar                                                                       |
| --------------------------------- | ----- | ---------------------------------------------------------------------------- |
| `AskAssistantValidatorTests.cs`   | 12    | Gränser för fråga och historik, otillåtna roller                             |
| `AssistantContextBuilderTests.cs` | 7     | Rubriker, veckodag, öppettider, resursstatus, arkiverade resurser            |
| `AskAssistantHandlerTests.cs`     | 14    | Meddelandeordning, rollsäkerhet, historikgräns, streaming, avbrott, loggning |
| `AssistantRateLimiterTests.cs`    | 5     | Gräns, per användare, tidsfönster (med `FakeTimeProvider`)                   |

För att köra tester endast kopplade till AI:

```bash
cd tests/Innovia.Api.Tests
dotnet test --filter "FullyQualifiedName~AssistantTests"
```

alternativt för att köra samtliga tester för projektet:

```bash
cd tests/Innovia.Api.Tests
dotnet test

```

De körs också automatiskt i GitHub Actions (`.github/workflows/ci.yaml`) vid varje push och pull request mot `dev` och `main`.

Inga tester anropar OpenAI. En `FakeChatClient` ersätter den riktiga klienten, så testerna är gratis, snabba och ger samma resultat varje gång. ContextBuilder- och Handler-testerna läser riktig data ur en PostgreSQL i Docker (Testcontainers), så Docker måste vara igång.

---

## Felsökning

| Problem                                     | Lösning                                                                                                                                   |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| API:et startar inte: `OpenAI:ApiKey saknas` | Sätt nyckeln med user-secrets (native) eller `OPENAI_API_KEY` i `.env` (Docker).                                                          |
| Bollen syns inte                            | Du är inte inloggad. Widgeten visas bara för inloggade.                                                                                   |
| Röd banner direkt / CORS-fel i konsolen     | Kontrollera att `Cors:FrontendOrigin` matchar frontendens adress.                                                                         |
| 401 på `/hubs/chat`                         | Kör frontend och backend på samma schema (båda `http` eller båda `https`), se huvud-README.                                               |
| Konstigt svar                               | Öppna `GET /assistant/context` som admin (Scalar) och se exakt vad Nova läser. Kan du inte svara utifrån texten kan Nova det inte heller. |
| Vilka frågor kunde Nova inte svara på?      | Sök efter `Nova kunde inte svara på frågan` i API-terminalens logg.                                                                       |
