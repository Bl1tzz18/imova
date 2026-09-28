using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Imova.Application.Features.SavedSearches;

// The token in an alert email's "stop these emails" link, so turning alerts off works in one click
// without signing in. A data-protection payload of the saved search's id — it can't be forged or
// pointed at another search, and doesn't expire (old emails keep working). The key ring is shared
// by the API and the Worker (both use ApplicationName "Imova.Api" and the DataProtectionKeys table).
public class SavedSearchUnsubscribeTokens(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Imova.SavedSearches.Unsubscribe");

    public string Create(Guid savedSearchId) => _protector.Protect(savedSearchId.ToString("N"));

    public bool IsValid(Guid savedSearchId, string token)
    {
        try
        {
            return _protector.Unprotect(token) == savedSearchId.ToString("N");
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
