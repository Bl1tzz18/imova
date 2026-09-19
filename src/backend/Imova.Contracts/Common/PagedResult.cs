namespace Imova.Contracts.Common;

// No pagination pattern existed anywhere in the backend before this — every other list endpoint
// (GetProperties, GetMyProperties, GetFavorites) returns everything unpaginated. Introduced here
// because the moderation queue is the first endpoint where that stops being reasonable.
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
