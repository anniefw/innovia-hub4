Steg 5: Testa realtid utan din app (cirka 30 min)
Öppna client/index.html med Live Server (port 5500, som är den port CORS redan tillåter) och kontrollera att measurementReceived loggas i konsolen.
Om något sedan krånglar i din app vet du då att felet sitter hos dig och inte i IoT-tjänsterna.
Klart när: konsolen visar live-värden för olika deviceId.

Steg 6: Backend-slicen Features/Sensors/ListSensors (cirka 2–3 h)
IDeviceRegistryClient som typad HttpClient. BaseUrl och TenantId läggs i appsettings.json under en egen sektion.
Handler, Endpoint och Response enligt ert vanliga mönster, med egna Response-records i stället för DeviceRegistrys DTO:er.
Om DeviceRegistry ligger nere: vad ska din endpoint svara? Svar: ett kontrollerat fel via din Result-typ, till exempel 503 Service Unavailable, inte en 500 med stacktrace. Fånga HttpRequestException i klienten.
Klart när: din egen endpoint returnerar de 10 enheterna i Swagger eller Scalar.

Steg 7: Sensorsidan i frontend, bara listan (cirka 2 h)
api/sensors.ts, hooks/useSensors.ts, SensorsPage.tsx, en route och en länk i navigationen.
Klart när: sidan visar 10 kort med till exempel serial, modell och rum, med “–” som värde.

Steg 8: Realtid i frontend (cirka 2–3 h)
Lägg till din Vite-origin (troligen http://localhost:5173) i CORS-policyn i Realtime.Hub.
lib/sensorHubConnection.ts (samma mönster som resourceHubConnection.ts): anslut, JoinTenant("innovia") och joina igen vid reconnect.
useSensorUpdates: spara senaste värdet per deviceId och typ i state eller via setQueryData, inte via invalidateQueries.
Varför inte invalidera som för bokningar? Svar: med 10 enheter × 2 typer var 10:e sekund blir det cirka 2 refetchar per sekund helt i onödan. Eventet innehåller redan hela värdet, så skriv in det direkt.
Klart när: “23,4 °C · 950 ppm” uppdateras live på varje kort. Nu är G klart funktionellt.

Steg 9 (valfritt): Startvärden
Utan det här är korten tomma i upp till 10 sekunder efter att sidan laddats. Du kan hämta senaste värdet via Portal.Adapter /measurements. Det är snyggt men inget krav, så hoppa över det om tiden är knapp.

Steg 10: Implementation.md för G och ett test från noll (cirka 1–2 h)
Stoppa allt och följ dina egna “Så kör du”-instruktioner från början. Det är så läraren kommer att köra projektet.
Klart när: allt fungerar enbart genom att följa dokumentet.

VG-kraven

Steg 11: Regler i Rules.Engine (cirka 2 h)
Lägg till Op, Threshold, Enabled och TenantSlug på RuleRow, samt Value och Message på AlertRow.
Endpoints: POST /rules, GET /rules och GET /alerts. Ge tjänsten en fast port i en appsettings.json.
Varför TenantSlug på regeln? Svar: hubbens grupper heter tenant:{slug}, men mätvärden har bara tenant-GUID, och DeviceRegistry saknar uppslag på GUID. Att spara slug på regeln är det enklaste sättet att komma runt det.

Steg 12: Utvärdering i workern (cirka 2 h)
RulesWorker är singleton men tar emot scoped DbContexts i konstruktorn. Varför är det fel? Svar: samma DbContext lever då för evigt, delas mellan alla loopvarv och cachar gammal data. Injicera IServiceScopeFactory och skapa ett nytt scope per varv.
Per regel: hämta senaste mätvärdet för enheten och typen, jämför med operatorn och lägg in en cooldown (till exempel 1 minut) så att det inte spammas larm.

Steg 13: alertRaised via Realtime.Hub (cirka 1 h)
Lägg till en hubbmetod, till exempel PublishAlert(alert), som skickar alertRaised till tenant:{slug}. Workern anropar den via sin HubConnection.
Klart när: client/index.html (med en extra connection.on("alertRaised", …)) loggar larmet.

Steg 14: Larm i frontend (cirka 1–2 h)
Lyssna på alertRaised i samma sensorHubConnection och visa en banner eller toast, och markera gärna kortet rött.
Tips för demon: sätt en regel som “temperature > 22” så att den slår till direkt.

Steg 15: Implementation.md för VG och genrep (cirka 1–2 h)
