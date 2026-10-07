import { useCallback, useEffect, useRef, useState } from "react";
import {
  ensureChatHubStarted,
  getChatHubConnection,
  streamAsk,
  type ChatRole,
  type ChatTurn,
} from "../lib/chatHubConnection";
import type { ISubscription } from "@microsoft/signalr";

//Konstanter
const MAX_HISTORY = 6;
const ERROR_MESSAGE =
  "Nova har gått vilse i Hubströmmen! Försök om en stund igen.";
const HUB_EXCEPTION_MARKER = "HubException: ";

//Typer
export type AssistantMessage = {
  id: string;
  role: ChatRole;
  text: string;
};

//Hjälpfunktion
// Plockar ut meddelandet från en HubException, om felet var en sådan. Andra fel (anslutningen bröts, servern nere) → null → generellt felmeddelande.
function extractHubMessage(err: unknown): string | null {
  if (!(err instanceof Error)) return null;

  const index = err.message.indexOf(HUB_EXCEPTION_MARKER);
  if (index === -1) return null;

  return err.message.slice(index + HUB_EXCEPTION_MARKER.length);
}

export function useAssistantChat() {
  //State: det widgeten ritar ut
  const [messages, setMessages] = useState<AssistantMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);

  //Ref - pågående ström, använder ref istället för state eftersom byta prenumeration inte ska rita om. Används för att avrbyta.
  const subscriptionRef = useRef<ISubscription<string> | null>(null);

  //Städa när komponenten försvinner: avbryt pågående ström
  useEffect(() => {
    return () => subscriptionRef.current?.dispose();
  }, []);

  //Hjälpfunktioner

  const appendToMessage = (id: string, chunk: string) => {
    setMessages((prev) =>
      prev.map((m) => (m.id === id ? { ...m, text: m.text + chunk } : m)),
    );
  };

  // Något gick fel: ta bort Novas meddelande om det är tomt,
  // behåll det om en del av svaret hann komma fram.
  const handleFailure = (assistantId: string, err: unknown) => {
    console.error("Nova failed to answer.", err);
    setMessages((prev) =>
      prev.filter((m) => m.id !== assistantId || m.text !== ""),
    );
    setError(extractHubMessage(err) ?? ERROR_MESSAGE);
    setIsStreaming(false);
    subscriptionRef.current = null;
  };
  //Send: ställ en fråga

  const send = useCallback(
    async (rawQuestion: string) => {
      const question = rawQuestion.trim();

      //tom fråga eller redan svar på väg = gör inget
      if (!question || isStreaming) return;

      setError(null);

      //1. Historik: de senaste meddelandena före nya frågan
      const history: ChatTurn[] = messages
        .filter((m) => m.text.trim() !== "")
        .slice(-MAX_HISTORY)
        .map((m) => ({ role: m.role, text: m.text }));

      //2. Lägg till frågan + ett tomt svar från Nova som fylls på bit för bit
      const userMessage: AssistantMessage = {
        id: crypto.randomUUID(),
        role: "user",
        text: question,
      };
      const assistantId = crypto.randomUUID();

      setMessages((prev) => [
        ...prev,
        userMessage,
        { id: assistantId, role: "assistant", text: "" },
      ]);
      setIsStreaming(true);

      try {
        //3. Anslut (gör inget om redan ansluten)
        const conn = getChatHubConnection();
        await ensureChatHubStarted(conn);

        //4. Strömma svaret
        subscriptionRef.current = streamAsk(conn, {
          question,
          history,
        }).subscribe({
          // Körs för varje "yield return chunk" i ChatHub
          next: (chunk) => appendToMessage(assistantId, chunk),

          // Körs när ChatHubs loop är klar
          complete: () => {
            subscriptionRef.current = null;
            setIsStreaming(false);
          },

          // Körs om ChatHub kastar HubException, eller om anslutningen bryts
          error: (err) => handleFailure(assistantId, err),
        });
      } catch (err) {
        // Anslutningen gick inte att starta (t.ex. utloggad, backend nere)
        handleFailure(assistantId, err);
      }
    },
    [messages, isStreaming],
  );

  //Stop: avrbyt ett svar som håller på att skrivas
  const stop = useCallback(() => {
    subscriptionRef.current?.dispose(); //Servern avbryter OpenAI-anropet
    subscriptionRef.current = null;
    setIsStreaming(false);
  }, []);

  //reset: börja om med tom konversation
  const reset = useCallback(() => {
    stop();
    setMessages([]);
    setError(null);
  }, [stop]);

  //Det widgeten får tillgång till
  return { messages, isStreaming, error, send, stop, reset };
}
