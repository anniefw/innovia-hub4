import { useEffect, useState } from "react";
import {
  ensureStarted,
  getSensorHubConnection,
  isRealtimeConfigured,
  joinTenant,
} from "../lib/sensorHubConnection";
import type { MeasurementReceived, SensorReading } from "../api/types";

const MEASUREMENT_RECEIVED = "measurementReceived";

export type SensorConnectionStatus = "connecting" | "live" | "offline";

export function useSensorReadings() {
  const [readings, setReadings] = useState<Record<string, SensorReading>>({});
  const [status, setStatus] = useState<SensorConnectionStatus>(
    isRealtimeConfigured ? "connecting" : "offline",
  );

  useEffect(() => {
    if (!isRealtimeConfigured) return;

    const conn = getSensorHubConnection();

    function handleMeasurement(m: MeasurementReceived) {
      setReadings((prev) => ({
        ...prev,
        [m.deviceId]: { ...prev[m.deviceId], [m.type]: m.value, time: m.time },
      }));
    }

    conn.on(MEASUREMENT_RECEIVED, handleMeasurement);

    (async () => {
      try {
        await ensureStarted(conn);
        await joinTenant(conn);
        setStatus("live");
      } catch (err) {
        console.error("Could not connect to Realtime.Hub", err);
        setStatus("offline");
      }
    })();

    return () => {
      conn.off(MEASUREMENT_RECEIVED, handleMeasurement);
    };
  }, []);

  return { readings, status, isLive: status === "live" };
}
