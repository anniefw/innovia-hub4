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
    // funktion som körs när ett larm kommer in: lägg det nya larmet FÖRST i listan, behåll bara de X senaste
    function handleAlert(alert: AlertRaised) {
      setAlerts((prev) => [alert, ...prev].slice(0, MAX_ALERTS));
    }
    // börja lyssna på eventet
    conn.on(ALERT_RAISED, handleAlert);
    //starta anslutningen och gå med i tenantens grupp
    (async () => {
      try {
        await ensureStarted(conn);
        await joinTenant(conn);
      } catch (err) {
        console.error("Could not connect to Realtime.Hub for alerts", err);
      }
    })();
    // städa upp: sluta lyssna
    return () => {
      conn.off(ALERT_RAISED, handleAlert);
    };
  }, []);

  //Ta bort alarm
  const dismiss = useCallback((id: string) => {
    setAlerts((prev) => prev.filter((a) => a.id !== id));
  }, []);

  return { alerts, dismiss };
}
