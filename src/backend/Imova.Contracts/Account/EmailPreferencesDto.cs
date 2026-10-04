namespace Imova.Contracts.Account;

// Which optional emails the user wants. FavoriteUpdates: a listing they saved changed price or is
// no longer available. (Saved-search alerts are set per search; account emails can't be turned off.)
public record EmailPreferencesDto(bool FavoriteUpdates);

