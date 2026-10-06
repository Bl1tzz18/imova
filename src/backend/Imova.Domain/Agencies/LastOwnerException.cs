namespace Imova.Domain.Agencies;

// An agency always keeps at least one Owner: its last Owner can't leave, be removed or step down
// until they've made someone else an Owner.
public sealed class LastOwnerException()
    : InvalidOperationException("An agency must keep at least one owner. Make someone else an owner first.");
