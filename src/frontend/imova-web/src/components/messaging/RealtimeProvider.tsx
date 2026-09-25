"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { getRealtimeToken } from "@/lib/messaging/actions";
import { createMessagingConnection, RealtimeEvents, type RealtimeEvent } from "@/lib/messaging/realtime";

type Handler = (payload: unknown) => void;

type RealtimeContextValue = {
  userId: string | null;
  connection: HubConnection | null;
  connected: boolean;
  unreadCount: number;
  setUnreadCount: (count: number) => void;
  subscribe: (event: RealtimeEvent, handler: Handler) => () => void;
};

const RealtimeContext = createContext<RealtimeContextValue>({
  userId: null,
  connection: null,
  connected: false,
  unreadCount: 0,
  setUnreadCount: () => {},
  subscribe: () => () => {},
});

// One realtime connection per tab for a logged-in user (none when logged out). Components listen
// through useRealtimeEvent; the header badge reads unreadCount, which the server pushes on every
// change and which is re-seeded from the page render (initialUnreadCount) on navigation.
export function RealtimeProvider({
  userId,
  initialUnreadCount,
  children,
}: {
  userId: string | null;
  initialUnreadCount: number;
  children: ReactNode;
}) {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  const [unreadCount, setUnreadCount] = useState(initialUnreadCount);
  const handlers = useRef(new Map<RealtimeEvent, Set<Handler>>());

  useEffect(() => setUnreadCount(initialUnreadCount), [initialUnreadCount]);

  useEffect(() => {
    if (!userId) return;

    const hub = createMessagingConnection(async () => (await getRealtimeToken()) ?? "");
    for (const event of Object.values(RealtimeEvents)) {
      hub.on(event, (payload: unknown) => {
        if (event === RealtimeEvents.UnreadCountChanged) {
          setUnreadCount((payload as { count: number }).count);
        }
        handlers.current.get(event)?.forEach((handler) => handler(payload));
      });
    }
    hub.onreconnecting(() => setConnected(false));
    hub.onreconnected(() => setConnected(true));
    hub.onclose(() => setConnected(false));

    let stopped = false;
    hub
      .start()
      .then(() => {
        if (!stopped) setConnected(true);
      })
      // Offline or the API is down: the page still works, just without live updates.
      .catch(() => setConnected(false));
    setConnection(hub);

    return () => {
      stopped = true;
      setConnection(null);
      setConnected(false);
      void hub.stop();
    };
  }, [userId]);

  const subscribe = useCallback((event: RealtimeEvent, handler: Handler) => {
    const set = handlers.current.get(event) ?? new Set<Handler>();
    set.add(handler);
    handlers.current.set(event, set);
    return () => {
      set.delete(handler);
    };
  }, []);

  const value = useMemo(
    () => ({ userId, connection, connected, unreadCount, setUnreadCount, subscribe }),
    [userId, connection, connected, unreadCount, subscribe],
  );

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

export function useRealtime() {
  return useContext(RealtimeContext);
}

// Calls the latest `handler` for every `event` while the component is mounted.
export function useRealtimeEvent<T>(event: RealtimeEvent, handler: (payload: T) => void) {
  const { subscribe } = useRealtime();
  const latest = useRef(handler);
  useEffect(() => {
    latest.current = handler;
  });
  useEffect(() => subscribe(event, (payload) => latest.current(payload as T)), [event, subscribe]);
}
