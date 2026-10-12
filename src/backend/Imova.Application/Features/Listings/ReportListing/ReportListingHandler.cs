using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.ReportListing;

public class ReportListingHandler(
    IApplicationDbContext dbContext, TimeProvider timeProvider, ListingReportOptions options, IDatabaseErrors databaseErrors)
    : IRequestHandler<ReportListingCommand, ReportListingResultDto?>
{
    // One open report per reporter and listing (ListingReportConfiguration).
    public const string OpenReportIndex = "IX_ListingReports_ListingId_ReporterUserId";

    public async Task<ReportListingResultDto?> Handle(ReportListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.AsNoTracking().WherePublic(dbContext)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        if (await ListingAccess.IsOwnedByAsync(dbContext, listing, request.UserId, cancellationToken))
        {
            throw new ValidationException(
                [CodedFailure.Of(nameof(request.ListingId), "You can't report your own listing.", ErrorCodes.ReportOwnListing)]);
        }

        var now = timeProvider.GetUtcNow();

        // Reporting again while the first report is still open changes it — one person is one voice
        // in the admin queue, however many times they press the button.
        var open = await dbContext.ListingReports.FirstOrDefaultAsync(
            r => r.ListingId == listing.Id && r.ReporterUserId == request.UserId && r.ResolvedAt == null, cancellationToken);
        if (open is not null)
        {
            open.Amend(request.Reason, request.Details, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ReportListingResultDto(Amended: true);
        }

        var reportedToday = await dbContext.ListingReports.CountAsync(
            r => r.ReporterUserId == request.UserId && r.CreatedAt > now.AddDays(-1), cancellationToken);
        if (reportedToday >= options.MaxPerDay)
        {
            throw new TooManyRequestsException(
                $"You can report at most {options.MaxPerDay} listings a day. Please try again tomorrow.",
                ErrorCodes.ReportLimitReached,
                CodedFailure.Params(("max", options.MaxPerDay)));
        }

        dbContext.ListingReports.Add(ListingReport.Create(listing, request.UserId, request.Reason, request.Details, now));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (databaseErrors.IsUniqueViolation(ex, OpenReportIndex))
        {
            // The same person's other submission (a double click) saved its report between our check and
            // our save. Ours becomes what pressing again does: an amendment of that open report — the
            // same answer a second, sequential click gets.
            dbContext.DiscardChanges();
            var saved = await dbContext.ListingReports.FirstAsync(
                r => r.ListingId == listing.Id && r.ReporterUserId == request.UserId && r.ResolvedAt == null, cancellationToken);
            saved.Amend(request.Reason, request.Details, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ReportListingResultDto(Amended: true);
        }

        return new ReportListingResultDto(Amended: false);
    }
}
