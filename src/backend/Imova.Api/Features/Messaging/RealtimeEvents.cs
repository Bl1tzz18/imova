namespace Imova.Api.Features.Messaging;

// Names of the events MessagingHub sends to browsers (mirrored in the frontend's
// lib/messaging/realtime.ts).
public static class RealtimeEvents
{
    public const string MessageReceived = "MessageReceived";
    public const string MessageStatusChanged = "MessageStatusChanged";
    public const string UnreadCountChanged = "UnreadCountChanged";
    public const string Typing = "Typing";
    public const string PresenceChanged = "PresenceChanged";
}

public static class RealtimeAuth
{
    // The JWT scheme the hub accepts: short-lived tokens with their own audience
    // (JwtTokenGenerator.GenerateRealtimeToken), never the regular session token.
    public const string Scheme = "Realtime";
}
