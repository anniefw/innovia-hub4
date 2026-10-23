import { useCallback, useEffect, useState } from "react";
import type { AlertRaised } from "../api/types";
import {
  ensureStarted,
  getSensorHubConnection,
  joinTenant,
} from "../lib/sensorHubConnection";

const ALERT_RAISED = "alertRaised";
const MAX_ALERTS = 5;

export function useSensorAlerts() {
  const [alerts, setAlerts] = useState<AlertRaised[]>([]);

  useEffect(() => {
    const conn = getSensorHubConnection();
    function handleAlert(alert: AlertRaised) {
      setAlerts((prev) => [alert, ...prev].slice(0, MAX_ALERTS));
    }

    conn.on(ALERT_RAISED, handleAlert);

    (async () => {
      try {
        await ensureStarted(conn);
        await joinTenant(conn);
      } catch (err) {
        console.error("Could not connect to Realtime.Hub for alerts", err);
      }
    })();

    return () => {
      conn.off(ALERT_RAISED, handleAlert);
    };
  }, []);

  const dismiss = useCallback((id: string) => {
    setAlerts((prev) => prev.filter((a) => a.id !== id));
  }, []);

  return { alerts, dismiss };
}
