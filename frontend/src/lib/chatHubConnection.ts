import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
  type IStreamResult,
} from "@microsoft/signalr";

//chatHubConnection.ts håller en delad SignalR-anslutning till Novas chatt (/hubs/chat).
// Den startar anslutningen och förnyar inloggningen vid behov, skickar frågor till backend och tar emot svaret som en ström av textbitar.
//Fungerar som ett mellanlager som både pratar med chatHub på backend, och hooken useAssistantChat på frontend.

// ── Konfiguration ─────────────────────────────────────────────────────────────
const BASE_URL = import.meta.env.VITE_API_URL ?? "https://localhost:7229";

// Namnet på hub-metoden i ChatHub.cs.
const AskMethod: string = "Ask";

// ── Typer ─────────────────────────────────────────────────────────────────────
// Speglar C#-recorden i AskAssistant/Request.cs.
// SignalR gör om { question, history } till Request(Question, History).

export type ChatRole = "user" | "assistant";

export type ChatTurn = {
  role: ChatRole;
  text: string;
};

export type AskRequest = {
  question: string;
  history: ChatTurn[];
};

// ── Singleton-tillstånd ───────────────────────────────────────────────────────

let connection: HubConnection | null = null;
let startPromise: Promise<void> | null = null;

// ── Bygg anslutningen ─────────────────────────────────────────────────────────

function buildConnection(): HubConnection {
  const conn = new HubConnectionBuilder()
    // withCredentials: skicka med inloggningscookien när anslutningen öppnas
    .withUrl(`${BASE_URL}/hubs/chat`, { withCredentials: true })
    .configureLogging(LogLevel.Information)
    .withAutomaticReconnect()
    .build();

  conn.onreconnecting((err) => {
    console.warn("Chat hub connection lost, attempting to reconnect", err);
  });

  // Ingen onreconnected-logik behövs: det finns inga grupper att gå med i igen.
  // Ett svar som höll på att strömmas när anslutningen bröts avbryts dock,
  // och det hanteras i hook via strömmens error-callback.

  conn.onclose((err) => {
    console.error("Chat hub connection closed permanently", err);
    startPromise = null; // så att ensureChatHubStarted kan starta om nästa gång
  });

  return conn;
}

// ── Token-förnyelse  ──────────────────

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

// Försök starta. Misslyckas det (troligen 401 för att token gått ut):
// förnya token och försök EN gång till.
async function startWithRefresh(conn: HubConnection): Promise<void> {
  try {
    await conn.start();
  } catch (firstError) {
    const refreshed = await refreshAccessToken();
    if (!refreshed) throw firstError; // gick inte att förnya → användaren måste logga in igen
    await conn.start();
  }
}

// ── Publika funktioner ────────────────────────────────────────────────────────

// Hämtar den delade anslutningen (skapas första gången)
export function getChatHubConnection(): HubConnection {
  if (!connection) {
    connection = buildConnection();
  }
  return connection;
}

// Ser till att anslutningen är igång. Anropas innan varje fråga.
// Eget namn (inte bara "ensureStarted") så att den inte krockar med
// resourceHubConnection om båda importeras i samma fil.
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

// Ställer en fråga till Nova och returnerar strömmen med textbitar.
// Typad wrapper runt connection.stream, så att hooken inte behöver känna
// till metodnamnet "Ask" eller hur argumentet ska se ut.
export function streamAsk(
  conn: HubConnection,
  request: AskRequest,
): IStreamResult<string> {
  return conn.stream<string>(AskMethod, request);
}
