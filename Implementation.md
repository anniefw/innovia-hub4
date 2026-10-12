Filen beskriver vilka ändringar och var ni har genomfört under projektet

Ändringar kopplade till G-krav
Minst 10 registrerade enheter i DeviceRegistry.

Alla enheter ska ta emot mock-data via Edge.Simulator.

Alla enheter ska kunna hämtas via DeviceRegistry API och visas i er egen portal.

Realtidsdata ska presenteras för enheterna med hjälp av SignalR + Realtime.Hub i er egen portal.

Ändringar kopplade till VG-krav
Bygga en enkel prototyp av Rules.Engine som:

Låter en regel skapas för en enhet (t.ex. ”om temperaturen är högre än 28°C”).

När mätvärden kommer in (via Ingest och Edge.Simulator), utvärderas regeln.

Om regeln slår till, skickas en alert via Realtime.Hub som kan fångas i portalen.

Minimikrav för VG
Skapa regel: Skapa regeler eller via enkel endpoint (t.ex. POST /rules).

Utvärdering: Regeln jämför senaste mätvärdet för enheten mot ett tröskelvärde.

Alert: Om tröskeln överskrids skickas ett SignalR-event (alertRaised) till tenantens grupp i Realtime.Hub.

Implementering i ert projekt: Plattformen kan ta emot alertRaised och skriva ut varningen i UI:t.
