using Imova.Contracts.Listings;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.PriceHistory;

// What visitors are told about a listing's price changes (the rows are ListingPriceChange).
//
// Only changes since the listing was first published count — what an owner tried while it was a
// draft or in review was never public. "Preț redus" is shown while the latest change was a drop
// (or the last of several drops in a row), made within ReductionShownFor, of at least
// MinReductionPercent: a raise after a drop ends it, an old drop stops being news, and knocking 1 €
// off doesn't earn a badge. It compares with the price before the first of those drops, so
// 82 000 → 80 000 → 79 500 reads "was 82 000, −3%".
public static class ListingPriceHistory
{
    public static readonly TimeSpan ReductionShownFor = TimeSpan.FromDays(30);

    public const int MinReductionPercent = 1;

    // The detail view's list stops at the latest this many (an owner fiddling with the price daily
    // shouldn't make the page endless).
    public const int MaxEntries = 20;

    // The public changes, oldest first.
    public static IReadOnlyList<ListingPriceChange> PublicChanges(IEnumerable<ListingPriceChange> changes, DateTimeOffset? publishedAt) =>
        publishedAt is null
            ? []
            : changes.Where(c => c.ChangedAt >= publishedAt).OrderBy(c => c.ChangedAt).ToList();

    public static PriceReductionDto? Reduction(IReadOnlyList<ListingPriceChange> publicChanges, Price current, DateTimeOffset now)
    {
        if (publicChanges.Count == 0)
        {
            return null;
        }

        var latest = publicChanges[^1];
        if (!IsDrop(latest) || now - latest.ChangedAt > ReductionShownFor)
        {
            return null;
        }

        var firstDrop = latest;
        for (var i = publicChanges.Count - 2; i >= 0 && IsDrop(publicChanges[i]); i--)
        {
            firstDrop = publicChanges[i];
        }

        var percent = -ChangePercent(firstDrop.OldAmount, firstDrop.OldCurrency, firstDrop.OldPriceEur, current.Amount, current.Currency, current.PriceEur);
        return percent < MinReductionPercent
            ? null
            : new PriceReductionDto(firstDrop.OldAmount, firstDrop.OldCurrency.ToString(), percent, latest.ChangedAt);
    }

    public static PriceHistoryDto? History(IReadOnlyList<ListingPriceChange> publicChanges)
    {
        if (publicChanges.Count == 0)
        {
            return null;
        }

        var shown = publicChanges.TakeLast(MaxEntries).ToList();
        return new PriceHistoryDto(
            StartsAtPublication: shown.Count == publicChanges.Count,
            shown.Select(c => new PriceChangeDto(
                    c.OldAmount,
                    c.OldCurrency.ToString(),
                    c.NewAmount,
                    c.NewCurrency.ToString(),
                    ChangePercent(c.OldAmount, c.OldCurrency, c.OldPriceEur, c.NewAmount, c.NewCurrency, c.NewPriceEur),
                    c.ChangedAt))
                .ToList());
    }

    // In the listing's own currency when it didn't change (exchange rates moving would otherwise
    // nudge it), else in EUR. Signed, rounded to a whole percent.
    public static int ChangePercent(
        decimal oldAmount, Currency oldCurrency, decimal oldPriceEur, decimal newAmount, Currency newCurrency, decimal newPriceEur)
    {
        var (from, to) = oldCurrency == newCurrency ? (oldAmount, newAmount) : (oldPriceEur, newPriceEur);
        return from <= 0 ? 0 : (int)Math.Round((to - from) / from * 100, MidpointRounding.AwayFromZero);
    }

    private static bool IsDrop(ListingPriceChange change) =>
        change.OldCurrency == change.NewCurrency
            ? change.NewAmount < change.OldAmount
            : change.NewPriceEur < change.OldPriceEur;
}
