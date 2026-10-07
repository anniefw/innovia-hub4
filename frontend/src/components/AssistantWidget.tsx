import { useEffect, useRef, useState } from "react";
import { useAssistantChat } from "../hooks/useAssistantChat";
import { useAuth } from "../auth/AuthContext";

const SUGGESTIONS = [
  "Får jag ta med gäster?",
  "Finns det kaffe?",
  "När kan jag boka mötesrum?",
  "Hur funkar VR-headsetet?",
];

const MAX_QUESTION_LENGTH = 500;

const GRADIENT = "bg-gradient-to-br from-blue-300 via-white to-teal-400";

export default function AssistantWidget() {
  const { messages, isStreaming, error, send, stop, reset } =
    useAssistantChat();

  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState("");

  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const lastMessage = messages[messages.length - 1];
  const isWaitingForFirstChunk =
    isStreaming && lastMessage?.role === "assistant" && lastMessage.text === "";

  const canSend = input.trim() !== "" && !isStreaming;

  // ── Stängd: rund flytande boll ──────────────────────────────────────────────
  if (!isOpen) {
    return (
      <div className="nova-pulse fixed bottom-6 right-6 z-50">
        <button
          onClick={() => setIsOpen(true)}
          aria-label="Öppna Nova, husassistenten"
          className={`flex h-20 w-20 flex-col items-center justify-center rounded-full ${GRADIENT} text-slate-800 shadow-xl transition-transform duration-200 hover:scale-110`}
        >
          <span className="text-3xl leading-none">💬</span>
          <span className="text-xs font-semibold">Nova</span>
        </button>
      </div>
    );
  }

  // ── Öppen: hela panelen ─────────────────────────────────────────────────────
  return (
    <div className="fixed bottom-6 right-6 z-50 flex h-[32rem] w-[calc(100vw-3rem)] max-w-sm flex-col overflow-hidden rounded-xl bg-white shadow-2xl">
      <div
        className={`flex items-center justify-between ${GRADIENT} px-4 py-3 text-slate-800`}
      >
        <span className="font-semibold">Nova – husassistent</span>
        <div className="flex items-center gap-3">
          {messages.length > 0 && (
            <button
              onClick={reset}
              aria-label="Ny konversation"
              className="rounded-full bg-white/80 px-3 py-1 text-xs font-semibold text-teal-700 shadow-sm transition-colors hover:bg-white"
            >
              Ny chatt
            </button>
          )}
          <button
            onClick={() => setIsOpen(false)}
            aria-label="Stäng chatten"
            className="text-lg"
          >
            ✕
          </button>
        </div>
      </div>

      {/* Meddelandelista */}
      <div className="flex-1 space-y-3 overflow-y-auto p-4">
        {messages.length === 0 && (
          <div className="space-y-3">
            <p className="text-sm text-gray-600">
              Hej! Jag heter Nova, din Ai-assistent som svarar på frågor om
              Innovia Hub. Vad undrar du?
            </p>
            <div className="flex flex-wrap gap-2">
              {SUGGESTIONS.map((s) => (
                <button
                  key={s}
                  onClick={() => send(s)}
                  className="rounded-full bg-purple-100 px-3 py-1 text-sm text-black hover:bg-purple-200"
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        )}

        {messages
          .filter((m) => m.text !== "")
          .map((m) => (
            <div
              key={m.id}
              className={
                m.role === "user" ? "flex justify-end" : "flex justify-start"
              }
            >
              <p
                className={`max-w-[85%] whitespace-pre-wrap rounded-2xl px-4 py-2 text-sm ${
                  m.role === "user"
                    ? "bg-teal-600 text-white"
                    : "bg-gray-100 text-gray-900"
                }`}
              >
                {m.text}
              </p>
            </div>
          ))}

        {isWaitingForFirstChunk && (
          <p className="text-sm italic text-gray-500">Nova skriver…</p>
        )}

        {error && <p className="text-sm text-red-600">{error}</p>}

        <div ref={bottomRef} />
      </div>

      {/* Inmatning */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (!input.trim()) return;
          send(input);
          setInput("");
        }}
        className="flex gap-2 border-t p-3"
      >
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          maxLength={MAX_QUESTION_LENGTH}
          disabled={isStreaming}
          placeholder="Skriv en egen fråga…"
          aria-label="Din fråga till Nova"
          className="flex-1 rounded-full border border-gray-300 bg-white px-4 py-2 text-sm text-black placeholder-gray-400 transition-colors focus:border-teal-500 focus:bg-gray-100 focus:outline-none disabled:bg-gray-100"
        />
        {isStreaming ? (
          <button
            type="button"
            onClick={stop}
            className="rounded-full bg-gray-500 px-4 py-2 text-sm text-white"
          >
            Stopp
          </button>
        ) : (
          <button
            type="submit"
            disabled={!canSend}
            className={`rounded-full bg-teal-600 px-4 py-2 text-sm font-semibold text-black hover:bg-teal-700 disabled:bg-gray-300 ${
              canSend ? "animate-pulse" : ""
            }`}
          >
            Fråga
          </button>
        )}
      </form>
    </div>
  );
}

// Visar Nova bara för inloggade användare
export function AssistantForLoggedIn() {
  const { user, loading } = useAuth();
  if (loading || !user) return null;
  return <AssistantWidget />;
}
