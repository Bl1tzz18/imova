using System.Globalization;

namespace Imova.Application.Common.Emails;

// How emails describe a listing in words (Romanian, like the emails themselves): its price, its
// kind and size, its place. Shared by the saved-search alerts and the saved-listing alerts.
public static class ListingEmailText
{
    private static readonly Dictionary<string, string> PropertyTypes = new()
    {
        ["Apartment"] = "Apartament",
        ["House"] = "Casă",
        ["Land"] = "Teren",
        ["Commercial"] = "Spațiu comercial",
        ["Garage"] = "Garaj",
        ["Room"] = "Cameră",
    };

    // "9 000 EUR" / "450 EUR / lună"
    public static string Price(decimal amount, string currency, string transactionType) =>
        $"{amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ')} {currency}"
        + (transactionType == "Rent" ? " / lună" : string.Empty);

    // "Garaj · Vânzare · 18 m²" (no area when it's unknown)
    public static string Facts(string propertyType, string transactionType, decimal areaM2) => string.Join(
        " · ",
        new[]
        {
            PropertyTypes.GetValueOrDefault(propertyType, propertyType),
            transactionType == "Rent" ? "Chirie" : "Vânzare",
            areaM2 > 0 ? $"{areaM2.ToString("0.##", CultureInfo.InvariantCulture)} m²" : null,
        }.Where(part => part is not null));

    // "Botanica, Chișinău" — the neighborhood or locality first, then the raion; null when nothing is set.
    public static string? Location(string? raionName, string? localitateName, string? chisinauSectorName)
    {
        var location = string.Join(
            ", ", new[] { chisinauSectorName ?? localitateName, raionName }.Where(n => !string.IsNullOrWhiteSpace(n)));
        return string.IsNullOrWhiteSpace(location) ? null : location;
    }
}
