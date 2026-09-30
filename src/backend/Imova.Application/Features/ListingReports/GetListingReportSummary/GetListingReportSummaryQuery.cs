using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.ListingReports.GetListingReportSummary;

// Admin-only: the counts behind the moderation page's "Reports" badge.
public record GetListingReportSummaryQuery(bool IsAdmin) : IRequest<ListingReportSummaryDto>;
