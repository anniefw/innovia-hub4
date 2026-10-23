import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
  type IStreamResult,
} from "@microsoft/signalr";

const BASE_URL = import.meta.env.VITE_API_URL ?? "https://localhost:7229";

const AskMethod: string = "Ask";

export type ChatRole = "user" | "assistant";

export type ChatTurn = {
  role: ChatRole;
  text: string;
};

export type AskRequest = {
  question: string;
  history: ChatTurn[];
};

export type ConnectionStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

let connection: HubConnection | null = null;
let startPromise: Promise<void> | null = null;
let status: ConnectionStatus = "idle";

const statusListeners = new Set<(s: ConnectionStatus) => void>();

function buildConnection(): HubConnection {
  const conn = new HubConnectionBuilder()
    .withUrl(`${BASE_URL}/hubs/chat`, { withCredentials: true })
    .configureLogging(LogLevel.Information)
    .withAutomaticReconnect()
    .build();

  conn.onreconnecting((err) => {
    console.warn("Chat hub connection lost, attempting to reconnect", err);
    setStatus("reconnecting");
  });

  conn.onreconnected(() => setStatus("connected"));

  conn.onclose((err) => {
    console.error("Chat hub connection closed permanently", err);
    startPromise = null;
    setStatus("disconnected");
  });

  return conn;
}

function setStatus(next: ConnectionStatus) {
  status = next;
  statusListeners.forEach((listener) => listener(next));
}

async function refreshAccessToken(): Promise<boolean> {
  try {
    const res = await fetch(`${BASE_URL}/auth/refresh`, {
      method: "POST",
      credentials: "include",
    });
    return res.ok;
  } catch {
    return false;
  }
}

async function startWithRefresh(conn: HubConnection): Promise<void> {
  try {
    await conn.start();
  } catch (firstError) {
    const refreshed = await refreshAccessToken();
    if (!refreshed) throw firstError;
    await conn.start();
  }
  setStatus("connected");
}
export function getChatHubConnection(): HubConnection {
  if (!connection) {
    connection = buildConnection();
  }
  return connection;
}

export function ensureChatHubStarted(conn: HubConnection): Promise<void> {
  if (conn.state === HubConnectionState.Connected) return Promise.resolve();
  if (!startPromise) {
    startPromise = startWithRefresh(conn).catch((err) => {
      startPromise = null;
      throw err;
    });
  }
  return startPromise;
}

export function streamAsk(
  conn: HubConnection,
  request: AskRequest,
): IStreamResult<string> {
  return conn.stream<string>(AskMethod, request);
}

export async function stopChatHubConnection(): Promise<void> {
  if (!connection) return;

  const conn = connection;
  connection = null;
  startPromise = null;

  try {
    await conn.stop();
  } catch (err) {
    console.warn("Could not cleanly stop chat hub connection", err);
  }

  setStatus("idle");
}

export function onChatHubStatusChange(
  listener: (s: ConnectionStatus) => void,
): () => void {
  statusListeners.add(listener);
  listener(status);
  return () => {
    statusListeners.delete(listener);
  };
}
