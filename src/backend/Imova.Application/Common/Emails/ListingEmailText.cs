using System.Globalization;

namespace Imova.Application.Common.Emails;

// How emails describe a listing in words, in the reader's language (see Languages): its price, its
// kind and size, its place. Shared by the saved-search alerts and the saved-listing alerts. The
// words are the web app's own (PropertyType.*, ListingType.*, PropertyCard.perMonth).
public static class ListingEmailText
{
    public static string PropertyType(string propertyType, string language) => propertyType switch
    {
        "Apartment" => Languages.Pick(language, "Apartament", "Apartment", "Квартира"),
        "House" => Languages.Pick(language, "Casă", "House", "Дом"),
        "Land" => Languages.Pick(language, "Teren", "Land", "Участок"),
        "Commercial" => Languages.Pick(language, "Spațiu comercial", "Commercial space", "Коммерческое помещение"),
        "Garage" => Languages.Pick(language, "Garaj", "Garage", "Гараж"),
        "Room" => Languages.Pick(language, "Cameră", "Room", "Комната"),
        _ => propertyType,
    };

    // "9 000 EUR" / "450 EUR / lună"
    public static string Price(decimal amount, string currency, string transactionType, string language) =>
        $"{amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ')} {currency}"
        + (transactionType == "Rent" ? Languages.Pick(language, " / lună", " / month", " / мес.") : string.Empty);

    public static string Deal(string transactionType, string language) => transactionType == "Rent"
        ? Languages.Pick(language, "Chirie", "Rent", "Аренда")
        : Languages.Pick(language, "Vânzare", "Sale", "Продажа");

    // "18 m²"; null when the area is unknown.
    public static string? Area(decimal areaM2) => areaM2 > 0 ? $"{areaM2.ToString("0.##", CultureInfo.InvariantCulture)} m²" : null;

    // "Garaj · Vânzare · 18 m²" (no area when it's unknown)
    public static string Facts(string propertyType, string transactionType, decimal areaM2, string language) => string.Join(
        " · ",
        new[]
        {
            PropertyType(propertyType, language),
            transactionType == "Rent"
                ? Languages.Pick(language, "Chirie", "Rent", "Аренда")
                : Languages.Pick(language, "Vânzare", "Sale", "Продажа"),
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
