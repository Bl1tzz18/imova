using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.ReportListing;

// A signed-in visitor reports an Active listing. UserId comes from the caller's JWT. Null = no
// such listing, or it isn't public (the caller can't see it, so can't report it either).
public record ReportListingCommand(Guid UserId, Guid ListingId, ListingReportReason Reason, string? Details)
    : IRequest<ReportListingResultDto?>;
