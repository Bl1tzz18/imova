namespace Imova.Domain.Properties;

// The only currencies a listing can be priced in — see CreatePropertyValidator/
// UpdatePropertyValidator's Currency rule. EUR is the default on the frontend (StepPriceContact),
// not enforced as a default here — this is purely the allow-list.
public static class SupportedCurrencies
{
    public const string Eur = "EUR";
    public const string Mdl = "MDL";
    public const string Usd = "USD";

    public static readonly IReadOnlyCollection<string> All = [Eur, Mdl, Usd];
}
