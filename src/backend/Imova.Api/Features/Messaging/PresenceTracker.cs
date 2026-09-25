using System.Collections.Concurrent;
using Imova.Application.Common.Interfaces;

namespace Imova.Api.Features.Messaging;

// Who has at least one open hub connection (a user may have several tabs). In-memory, so it is
// per API instance — enough while there's a single instance; scaling out needs a backplane.
public class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    public bool IsOnline(Guid userId) => _connections.TryGetValue(userId, out var ids) && ids.Count > 0;

    // True when this is the user's first connection (they just came online).
    public bool Connect(Guid userId, string connectionId)
    {
        var ids = _connections.GetOrAdd(userId, _ => []);
        lock (ids)
        {
            ids.Add(connectionId);
            return ids.Count == 1;
        }
    }

    // True when this was the user's last connection (they just went offline).
    public bool Disconnect(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var ids))
        {
            return false;
        }

        lock (ids)
        {
            return ids.Remove(connectionId) && ids.Count == 0;
        }
    }
}
