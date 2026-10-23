import { useEffect, useState } from "react";
import { getSensorHubConnection, joinTenant } from "../lib/sensorHubConnection";
import { ensureStarted } from "../lib/bookingHubConnection";
import type { MeasurementReceived, SensorReading } from "../api/types";

const MEASUREMENT_RECEIVED = "measurementReceived";

export function useSensorReadings() {
  const [readings, setReadings] = useState<Record<string, SensorReading>>({});
  const [isLive, setIsLive] = useState(false);

  useEffect(() => {
    const conn = getSensorHubConnection();

    function handleMeasurement(m: MeasurementReceived) {
      setReadings((prev) => ({
        ...prev,
        [m.deviceId]: {
          ...prev[m.deviceId],
          [m.type]: m.value,
          time: m.time,
        },
      }));
    }

    conn.on(MEASUREMENT_RECEIVED, handleMeasurement);

    (async () => {
      try {
        await ensureStarted(conn);
        await joinTenant(conn);
        setIsLive(true);
      } catch (err) {
        console.error("Could not connect to Realtime.Hub", err);
        setIsLive(false);
      }
    })();

    return () => {
      conn.off(MEASUREMENT_RECEIVED, handleMeasurement);
    };
  }, []);

  return { readings, isLive };
}
