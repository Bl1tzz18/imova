using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.ListingReports.GetReportedListings;

public class GetReportedListingsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetReportedListingsQuery, PagedResult<ReportedListingDto>>
{
    // A case: the listing, plus — for the history — the moment the decision closed its reports.
    private sealed record CaseKey(Guid ListingId, DateTimeOffset? ResolvedAt);

    public async Task<PagedResult<ReportedListingDto>> Handle(GetReportedListingsQuery request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        var (total, keys) = request.Resolved
            ? await ResolvedCasesAsync(request, cancellationToken)
            : await OpenCasesAsync(request, cancellationToken);
        if (keys.Count == 0)
        {
            return new PagedResult<ReportedListingDto>([], request.Page, request.PageSize, total);
        }

        var listingIds = keys.Select(k => k.ListingId).Distinct().ToList();
        var candidates = await dbContext.ListingReports.AsNoTracking()
            .Where(r => listingIds.Contains(r.ListingId) && (r.ResolvedAt != null) == request.Resolved)
            .ToListAsync(cancellationToken);
        var reportsByCase = candidates
            .GroupBy(r => new CaseKey(r.ListingId, r.ResolvedAt))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.UpdatedAt).ToList());

        var listings = await dbContext.Listings.AsNoTracking().Where(l => listingIds.Contains(l.Id)).ToListAsync(cancellationToken);
        var listingDtos = (await ListingDtoLoader.LoadAsync(
                dbContext, blobStorageService, listings, currentUserId: null, cancellationToken,
                includeContactDetails: true, viewerIsAdmin: true))
            .ToDictionary(l => l.Id);

        var reporterIds = candidates.Select(r => r.ReporterUserId).Distinct().ToList();
        var reporterRecords = await dbContext.ListingReports.AsNoTracking()
            .Where(r => reporterIds.Contains(r.ReporterUserId))
            .GroupBy(r => r.ReporterUserId)
            .Select(g => new { UserId = g.Key, Filed = g.Count(), Dismissed = g.Count(r => r.Outcome == ListingReportOutcome.Dismissed) })
            .ToDictionaryAsync(x => x.UserId, cancellationToken);
        var userIds = reporterIds.Concat(candidates.Where(r => r.ResolvedByUserId != null).Select(r => r.ResolvedByUserId!.Value)).Distinct().ToList();
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = new List<ReportedListingDto>();
        foreach (var key in keys)
        {
            // A listing deleted between the two queries takes its reports with it — skip the case.
            if (!listingDtos.TryGetValue(key.ListingId, out var listing) || !reportsByCase.TryGetValue(key, out var reports))
            {
                continue;
            }

            var reasons = reports
                .GroupBy(r => r.Reason)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                .Select(g => new ReportReasonCountDto(g.Key.ToString(), g.Count()))
                .ToList();

            // A case's reports were resolved together, so any of them carries the decision.
            var decided = reports[0];
            var resolution = decided.ResolvedAt is { } resolvedAt
                ? new ListingReportResolutionDto(
                    decided.Outcome!.Value.ToString(),
                    resolvedAt,
                    decided.ResolvedByUserId!.Value,
                    users.GetValueOrDefault(decided.ResolvedByUserId.Value)?.DisplayName,
                    decided.ResolutionNote)
                : null;

            items.Add(new ReportedListingDto(
                listing,
                reports.Count,
                reasons,
                reports.Min(r => r.CreatedAt),
                reports.Max(r => r.UpdatedAt),
                reports.Select(r =>
                {
                    var user = users.GetValueOrDefault(r.ReporterUserId);
                    var record = reporterRecords.GetValueOrDefault(r.ReporterUserId);
                    return new ListingReportItemDto(
                        r.Id,
                        r.Reason.ToString(),
                        r.Details,
                        r.CreatedAt,
                        r.UpdatedAt,
                        new ListingReporterDto(
                            r.ReporterUserId,
                            user?.DisplayName,
                            user?.Email,
                            IsDeleted: user is null,
                            record?.Filed ?? 1,
                            record?.Dismissed ?? 0));
                }).ToList(),
                resolution));
        }

        return new PagedResult<ReportedListingDto>(items, request.Page, request.PageSize, total);
    }

    private async Task<(int Total, List<CaseKey> Keys)> OpenCasesAsync(GetReportedListingsQuery request, CancellationToken cancellationToken)
    {
        var cases = dbContext.ListingReports.AsNoTracking()
            .Where(r => r.ResolvedAt == null)
            .GroupBy(r => r.ListingId)
            .Select(g => new { ListingId = g.Key, Count = g.Count(), Since = g.Min(r => r.CreatedAt) });

        var total = await cases.CountAsync(cancellationToken);
        var page = await cases
            .OrderByDescending(c => c.Count).ThenBy(c => c.Since).ThenBy(c => c.ListingId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return (total, page.Select(c => new CaseKey(c.ListingId, null)).ToList());
    }

    private async Task<(int Total, List<CaseKey> Keys)> ResolvedCasesAsync(GetReportedListingsQuery request, CancellationToken cancellationToken)
    {
        var cases = dbContext.ListingReports.AsNoTracking()
            .Where(r => r.ResolvedAt != null)
            .GroupBy(r => new { r.ListingId, r.ResolvedAt })
            .Select(g => new { g.Key.ListingId, g.Key.ResolvedAt });

        var total = await cases.CountAsync(cancellationToken);
        var page = await cases
            .OrderByDescending(c => c.ResolvedAt).ThenBy(c => c.ListingId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return (total, page.Select(c => new CaseKey(c.ListingId, c.ResolvedAt)).ToList());
    }
}
