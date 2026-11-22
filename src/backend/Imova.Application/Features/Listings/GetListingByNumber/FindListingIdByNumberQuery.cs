using MediatR;

namespace Imova.Application.Features.Listings.GetListingByNumber;

// A listing's short public number (the "ID 100015" on its page, and the start of its web address
// /property/100015-…) → its id, whatever its status. Who may see the listing is still decided by
// GetListingById / GetEndedListing, exactly as for a lookup by id.
public record FindListingIdByNumberQuery(long Number) : IRequest<Guid?>;
