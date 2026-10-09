using Imova.Contracts.Listings;

namespace Imova.Contracts.Agencies;

// GET /api/v1/agencies/{id}/listings: one page of one group (Group: Active | Unpublished | Ended),
// plus every group's count and whether something in it waits on the owner — counted after the search
// (?q=), like the management page's tabs show them.
public record AgencyListingsPageDto(
    IReadOnlyList<ListingDto> Items,
    string Group,
    int Page,
    int PageSize,
    int TotalCount,
    AgencyListingGroupDto Active,
    AgencyListingGroupDto Unpublished,
    AgencyListingGroupDto Ended);

public record AgencyListingGroupDto(int Count, bool NeedsAttention);
