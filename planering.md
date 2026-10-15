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

STEG 16: Ta bort onödig kommentarer från SAMTLIGA filer. Gå igenom hela projektet.

Fundering:
lägg till en rubrik högst upp i sensorer: Alla mätvärden är inom godkända spann. Vid alerts/fel ändras denna till att tyliggöra tex "AI-servern har icke godkända mätvärden. Grön text vid godkända mätvärden, gul text lätt förhöjd, röd text inte acceptabelt.

Fundering: Går det visa mätvärden i ett "roligare" kort eller mer kreativt? Cirklar?
