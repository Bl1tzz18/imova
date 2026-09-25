import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import { getBrowserApiUrl } from "@/lib/api/media";

// Event names the API's MessagingHub sends (Imova.Api.Features.Messaging.RealtimeEvents).
export const RealtimeEvents = {
  MessageReceived: "MessageReceived",
  MessageStatusChanged: "MessageStatusChanged",
  UnreadCountChanged: "UnreadCountChanged",
  Typing: "Typing",
  PresenceChanged: "PresenceChanged",
} as const;

export type RealtimeEvent = (typeof RealtimeEvents)[keyof typeof RealtimeEvents];

// tokenFactory is asked again on every (re)connect — realtime tokens are short-lived. No cookies
// are involved (withCredentials: false), so no CORS credentials either.
export function createMessagingConnection(tokenFactory: () => Promise<string>): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${getBrowserApiUrl()}/hubs/messaging`, { accessTokenFactory: tokenFactory, withCredentials: false })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();
}
