using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Imova.Application.Features.Favorites.Alerts;

// The token in a saved-listing email's "stop these emails" link, so turning them off works in one
// click without signing in. A data-protection payload of the user's id — it can't be forged or
// pointed at another account, and doesn't expire (old emails keep working). Same scheme as
// SavedSearchUnsubscribeTokens, with its own purpose so neither token works for the other.
public class FavoriteAlertUnsubscribeTokens(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Imova.Favorites.Unsubscribe");

    public string Create(Guid userId) => _protector.Protect(userId.ToString("N"));

    public bool IsValid(Guid userId, string token)
    {
        try
        {
            return _protector.Unprotect(token) == userId.ToString("N");
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
