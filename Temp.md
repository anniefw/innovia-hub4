### Temp

{
"id": "dbdc9044-78f7-40ce-84b0-8b0f5673bef1",
"name": "Innovia Hub",
"slug": "innovia"
}

Tenant-ID: dbdc9044-78f7-40ce-84b0-8b0f5673bef1

MÖTESRUM 1:

Code Details
201
Undocumented
Response body
Download
{
"id": "f2fd3c1d-9748-4143-9140-857b43d954bb",
"tenantId": "dbdc9044-78f7-40ce-84b0-8b0f5673bef1",
"roomId": "01a07b3f-2692-729d-908e-c8ba939bf455",
"model": "Acme CO2-Temp",
"serial": "dev-101",
"status": "active"
}

RESULTAT SQL QUERY
"01a07b3f-2692-7c1d-8132-200a83477da9" "AI-server" "AI-server"
"01a07b3f-2692-71c2-a9b3-7767dd48fe47" "Drop-in skrivbord 1" "Drop-in skrivbord"
"01a07b3f-2692-7b72-a452-d5054ad24f38" "Drop-in skrivbord 10" "Drop-in skrivbord"
"01a07b3f-2692-7283-a027-21784106dac5" "Drop-in skrivbord 11" "Drop-in skrivbord"
"01a07b3f-2692-7cf5-a0f1-d7767cf3aff3" "Drop-in skrivbord 12" "Drop-in skrivbord"
"01a07b3f-2692-7ecb-9253-f34c09b2f70d" "Drop-in skrivbord 13" "Drop-in skrivbord"
"01a07b3f-2692-7a3d-aa39-a437db048fb8" "Drop-in skrivbord 14" "Drop-in skrivbord"
"01a07b3f-2692-76bf-896c-4427b2a9c22a" "Drop-in skrivbord 15" "Drop-in skrivbord"
"01a07b3f-2692-7bfa-8327-f62aa96a18dd" "Drop-in skrivbord 2" "Drop-in skrivbord"
"01a07b3f-2692-7c04-a34e-46ba6935a173" "Drop-in skrivbord 3" "Drop-in skrivbord"
"01a07b3f-2692-77c7-98fb-76d67e28493d" "Drop-in skrivbord 4" "Drop-in skrivbord"
"01a07b3f-2692-7128-8479-564178c798c7" "Drop-in skrivbord 5" "Drop-in skrivbord"
"01a07b3f-2692-7913-8708-62c4acfa8edc" "Drop-in skrivbord 6" "Drop-in skrivbord"
"01a07b3f-2692-76c9-907a-66b7589f2bbd" "Drop-in skrivbord 7" "Drop-in skrivbord"
"01a07b3f-2692-72b8-8578-8728980869b4" "Drop-in skrivbord 8" "Drop-in skrivbord"
"01a07b3f-2692-7e57-aa87-7f183743c405" "Drop-in skrivbord 9" "Drop-in skrivbord"
"01a07b3f-2692-729d-908e-c8ba939bf455" "Mötesrum 1" "Mötesrum"
"01a07b3f-2692-7803-bdc5-4e07b12b462c" "Mötesrum 2" "Mötesrum"
"01a07b3f-2692-78d2-9557-4dd945592021" "Mötesrum 3" "Mötesrum"
"01a07b3f-2692-703c-8ef8-7c7828f040c6" "Mötesrum 4" "Mötesrum"
"01a07b3f-2692-7586-b050-c4c9e62c1427" "VR headset 1" "VR headset"
"01a07b3f-2692-7697-a5f9-0e1ce92ea0ee" "VR headset 2" "VR headset"
"01a07b3f-2692-7ab7-b3bc-814ef4968703" "VR headset 3" "VR headset"
"01a07b3f-2692-7d46-9385-29e68cd781e0" "VR headset 4" "VR headset"

## Bugglogg

Realtime.Hub.csproj refererade ett gammalt SignalR-paket från .NET Core 2.x (borttaget)
Realtime.Hub saknar Swagger
Ingest.Gateway kraschar om Realtime.Hub inte är igång, eftersom StartAsync() saknar try/catch. Startordningen är därför viktig
DeviceRegistry.Api tar emot entiteter direkt (mass assignment), så klienten kan sätta Id själv. Swaggers exempel-GUID orsakade dubblett
En dubblett ger 500 i stället för 409 Conflict (Postgres 23505 fångas inte)
Slug och Serial saknar unique-index
README nämner GET /api/tenants, som inte finns
deploy/docker-compose.yml saknar namngiven volym, så down raderar all IoT-data
Redis startas men används inte av någon tjänst
Innovia.Shared innehåller oanvända klasser (Measurement, SensorKind)
(ovan från steg 1- steg 3)
