# Edge.Simulator Dokumentation
# Edge.Simulator – Documentation

The **Edge.Simulator** is a console application that simulates IoT edge devices publishing telemetry data via MQTT. It is used for development and testing without requiring physical hardware sensors.

---

## Purpose
- Simulates IoT devices (e.g., temperature, CO₂ sensors).
- Publishes measurement data to an MQTT broker (Eclipse Mosquitto).
- Allows developers to test the full ingestion and real-time data flow through the Innovia Hub system.

---

## How It Works
1. Connects to the MQTT broker at `localhost:1883`.
2. Simulates a device with a specific **serial** (default: `dev-101`).
3. Every 10 seconds publishes a JSON payload with metrics such as `temperature` and `co2` to the topic:
   ```
   tenants/{tenantSlug}/devices/{serial}/measurements
   ```
4. The payload looks like:
   ```json
   {
     "deviceId": "dev-101",
     "apiKey": "dev-101-key",
     "timestamp": "2025-10-03T12:00:00Z",
     "metrics": [
       { "type": "temperature", "value": 22.5, "unit": "C" },
       { "type": "co2", "value": 950, "unit": "ppm" }
     ]
   }
   ```
5. Ingest.Gateway (via MQTT subscription) processes the message, stores it in PostgreSQL, and publishes it to Realtime.Hub.

---

## Running the Simulator
1. Make sure the broker (Mosquitto) is running via Docker:
   ```bash
   docker compose -f deploy/docker-compose.yml up -d mosquitto
   ```
2. Start the simulator:
   ```bash
   cd src/Edge.Simulator
   dotnet run
   ```
3. You should see logs such as:
   ```
   Edge.Simulator starting… connecting to MQTT at localhost:1883
   ✅ Connected to MQTT broker.
   [2025-10-03T12:00:00Z] Published to 'tenants/innovia/devices/dev-101/measurements': { …payload… }
   ```

---

## Customization
- **Interval**: The default interval is 10 seconds. Change in `Program.cs` (`Task.Delay(TimeSpan.FromSeconds(...))`).
- **Device Serial**: Change `deviceId` in `Program.cs` to simulate another device (e.g., `dev-102`).
- **Metrics**: Extend the metrics array to simulate more sensor values (humidity, light, etc.).
- **Multiple Devices**: Run multiple instances of the simulator with different serials to simulate multiple devices.

---

## Verification
- Subscribe to MQTT messages directly:
  ```bash
  mosquitto_sub -h localhost -t 'tenants/#' -v
  ```
- Check Ingest debug endpoint:
  ```bash
  curl "http://localhost:5102/ingest/debug/device/{DEVICE_GUID}"
  ```
- Query Portal.Adapter for measurements:
  ```bash
  curl "http://localhost:5104/portal/{TENANT_GUID}/devices/{DEVICE_GUID}/measurements"
  ```

---

## Notes
- The simulator is for **development and testing** only.
- Real hardware devices would replace the simulator in production.
- By using the simulator, you can validate the entire IoT flow: **Device → MQTT → Ingest → Database → Realtime → Portal**.