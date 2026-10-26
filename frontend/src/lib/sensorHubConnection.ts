import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

const REALTIME_URL: string | null =
  import.meta.env.VITE_REALTIME_URL ??
  (import.meta.env.DEV ? "http://localhost:5103" : null);

export const isRealtimeConfigured = REALTIME_URL !== null;

const TENANT_SLUG = import.meta.env.VITE_IOT_TENANT ?? "innovia";

let connection: HubConnection | null = null;
let startPromise: Promise<void> | null = null;

function buildConnection(): HubConnection {
  const conn = new HubConnectionBuilder()
    .withUrl(`${REALTIME_URL}/hub/telemetry`)
    .configureLogging(LogLevel.Information)
    .withAutomaticReconnect()
    .build();

  conn.onreconnecting((err) => {
    console.warn("Sensor hub connection lost, attempting to reconnect", err);
  });

  conn.onreconnected(async () => {
    try {
      await conn.invoke("JoinTenant", TENANT_SLUG);
    } catch (err) {
      console.error("Failed to rejoin tenant group", err);
    }
  });

  conn.onclose((err) => {
    console.error("Sensor hub connection closed permanently", err);
    startPromise = null;
  });

  return conn;
}

export function getSensorHubConnection(): HubConnection {
  if (!connection) {
    connection = buildConnection();
  }
  return connection;
}

export function ensureStarted(conn: HubConnection): Promise<void> {
  if (conn.state === HubConnectionState.Connected) return Promise.resolve();
  if (!startPromise) {
    startPromise = conn.start().catch((err) => {
      startPromise = null;
      throw err;
    });
  }
  return startPromise;
}

export async function joinTenant(conn: HubConnection) {
  await conn.invoke("JoinTenant", TENANT_SLUG);
}
