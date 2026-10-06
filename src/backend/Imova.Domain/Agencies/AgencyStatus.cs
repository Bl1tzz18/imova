namespace Imova.Domain.Agencies;

// A Deactivated agency is hidden from the public — its page and every one of its listings — but
// stays visible to its members, who can reactivate it.
public enum AgencyStatus
{
    Active = 1,
    Deactivated = 2,
}
