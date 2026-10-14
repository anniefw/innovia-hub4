import { useCallback, useEffect, useRef, useState } from "react";
import {
  ensureChatHubStarted,
  getChatHubConnection,
  onChatHubStatusChange,
  streamAsk,
  type ChatRole,
  type ChatTurn,
  type ConnectionStatus,
} from "../lib/chatHubConnection";
import type { ISubscription } from "@microsoft/signalr";

const MAX_HISTORY = 6;
const ERROR_MESSAGE =
  "Nova har gått vilse i Hubströmmen! Försök om en stund igen.";
const HUB_EXCEPTION_MARKER = "HubException: ";

export type AssistantMessage = {
  id: string;
  role: ChatRole;
  text: string;
};

function extractHubMessage(err: unknown): string | null {
  if (!(err instanceof Error)) return null;

  const index = err.message.indexOf(HUB_EXCEPTION_MARKER);
  if (index === -1) return null;

  return err.message.slice(index + HUB_EXCEPTION_MARKER.length);
}

export function useAssistantChat() {
  const [messages, setMessages] = useState<AssistantMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] =
    useState<ConnectionStatus>("idle");

  const subscriptionRef = useRef<ISubscription<string> | null>(null);

  useEffect(() => onChatHubStatusChange(setConnectionStatus), []);

  useEffect(() => {
    return () => subscriptionRef.current?.dispose();
  }, []);

  const appendToMessage = (id: string, chunk: string) => {
    setMessages((prev) =>
      prev.map((m) => (m.id === id ? { ...m, text: m.text + chunk } : m)),
    );
  };

  const handleFailure = (assistantId: string, err: unknown) => {
    console.error("Nova failed to answer.", err);
    setMessages((prev) =>
      prev.filter((m) => m.id !== assistantId || m.text !== ""),
    );
    setError(extractHubMessage(err) ?? ERROR_MESSAGE);
    setIsStreaming(false);
    subscriptionRef.current = null;
  };

  const send = useCallback(
    async (rawQuestion: string) => {
      const question = rawQuestion.trim();

      if (!question || isStreaming) return;

      setError(null);

      const history: ChatTurn[] = messages
        .filter((m) => m.text.trim() !== "")
        .slice(-MAX_HISTORY)
        .map((m) => ({ role: m.role, text: m.text }));

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
        const conn = getChatHubConnection();
        await ensureChatHubStarted(conn);

        subscriptionRef.current = streamAsk(conn, {
          question,
          history,
        }).subscribe({
          next: (chunk) => appendToMessage(assistantId, chunk),

          complete: () => {
            subscriptionRef.current = null;
            setIsStreaming(false);
          },

          error: (err) => handleFailure(assistantId, err),
        });
      } catch (err) {
        handleFailure(assistantId, err);
      }
    },
    [messages, isStreaming],
  );

  const stop = useCallback(() => {
    subscriptionRef.current?.dispose();
    subscriptionRef.current = null;
    setIsStreaming(false);
  }, []);

  const reset = useCallback(() => {
    stop();
    setMessages([]);
    setError(null);
  }, [stop]);

  return { messages, isStreaming, error, connectionStatus, send, stop, reset };
}
