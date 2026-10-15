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
        ...prev, //behåll alla andra sensorers värde
        [m.deviceId]: {
          ...prev[m.deviceId], //behåll den här sensors andra värde
          [m.type]: m.value, //uppdatera bara den typ som kom in
          time: m.time,
        },
      }));
    }

    //börja lyssna på eventet
    conn.on(MEASUREMENT_RECEIVED, handleMeasurement);

    //anslut och gå med i grupp
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

    //när sidan stängs
    return () => {
      conn.off(MEASUREMENT_RECEIVED, handleMeasurement);
    };
  }, []);

  return { readings, isLive };
}
