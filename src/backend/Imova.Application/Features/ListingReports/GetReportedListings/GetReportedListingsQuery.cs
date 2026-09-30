using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.ListingReports.GetReportedListings;

// Admin-only: the reported-listings queue, one case per listing.
//  - Open (Resolved = false): listings with open reports, most-reported first, then the one
//    waiting longest — so a listing many people flag rises to the top.
//  - History (Resolved = true): each decision (suspend / dismiss) with the reports it closed,
//    most recent first.
public record GetReportedListingsQuery(bool IsAdmin, bool Resolved = false, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ReportedListingDto>>;
