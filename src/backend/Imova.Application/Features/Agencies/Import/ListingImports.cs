using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Import;

// The Worker job behind the CSV import. Each run:
// 1. deletes uploaded files older than ListingImportJob.KeepFileFor (the results stay);
// 2. fails imports stuck in Processing for StaleAfter (the Worker stopped mid-way) — the rows already
//    saved stay, and importing the same file again carries on (rows are matched by external_ref);
// 3. takes the oldest waiting import (claimed by saving it as Processing — see the configuration's
//    concurrency token) and runs it row by row: parse (ImportRowParser), place on the map
//    (ImportGeolocator), photos + create/update (ImportListingWriter), one save per row so progress
//    shows and one bad row never undoes the others; then the counts and an email to the uploader.
// One import per run; the next waits for the next tick.
public sealed class ListingImports(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IGeocodingService geocodingService,
    ImportListingWriter writer,
    IEmailSender emailSender,
    AppOptions appOptions,
    TimeProvider timeProvider,
    ILogger<ListingImports> logger) : IScheduledJob
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromHours(2);

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await DeleteOldFilesAsync(cancellationToken);
        await FailStaleAsync(cancellationToken);

        var job = await dbContext.ListingImportJobs
            .Where(j => j.Status == ListingImportStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            return 0;
        }

        await ProcessAsync(job, cancellationToken);
        return 1;
    }

    private async Task ProcessAsync(ListingImportJob job, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var content = await ReadFileAsync(job, cancellationToken);
        var (file, fileError) = content is null
            ? (null, new ImportFileError(ErrorCodes.ImportFileMissing, "The uploaded file is gone."))
            : ImportCsvReader.Read(content);

        if (fileError is not null)
        {
            job.Fail(fileError.Code, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            await NotifyAsync(job, cancellationToken);
            return;
        }

        job.Start(file!.Rows.Count, now);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another Worker claimed it first.
            dbContext.DiscardChanges();
            return;
        }

        var jobId = job.Id;
        var agencyId = job.AgencyId;
        var context = await ImportContext.LoadAsync(dbContext, agencyId, job.UploadedByUserId ?? Guid.Empty, cancellationToken);
        var parser = new ImportRowParser(context, new ImportListingValidator(dbContext));
        var geolocator = new ImportGeolocator(geocodingService);
        var photoUploader = job.UploadedByUserId ?? context.DefaultAuthorId;

        int created = 0, updated = 0, failed = 0, warned = 0;
        foreach (var csvRow in file.Rows)
        {
            var row = await ImportRowAsync(csvRow, parser, geolocator, agencyId, jobId, photoUploader, cancellationToken);
            dbContext.ListingImportRows.Add(row);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Import {JobId}: saving row {Row} failed.", jobId, csvRow.RowNumber);
                dbContext.DiscardChanges();
                row = ListingImportRow.Failed(jobId, csvRow.RowNumber, csvRow.Get(ImportColumns.ExternalRef), [RowFailed()]);
                dbContext.ListingImportRows.Add(row);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            switch (row.Outcome)
            {
                case ListingImportRowOutcome.Created: created++; break;
                case ListingImportRowOutcome.Updated: updated++; break;
                default: failed++; break;
            }

            if (row.Outcome != ListingImportRowOutcome.Failed && row.HasWarnings)
            {
                warned++;
            }
        }

        var refs = file.Rows.Select(r => r.Get(ImportColumns.ExternalRef)).Where(r => r.Length > 0).Distinct().ToList();
        var notInFile = await dbContext.Listings.CountAsync(
            l => l.AgencyId == agencyId && l.Status == ListingStatus.Active && l.ExternalRef != null && !refs.Contains(l.ExternalRef),
            cancellationToken);

        dbContext.DiscardChanges();
        job = await dbContext.ListingImportJobs.FirstAsync(j => j.Id == jobId, cancellationToken);
        job.Complete(created, updated, failed, warned, notInFile, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAsync(job, cancellationToken);
    }

    // One row → its ListingImportRow (the listing, if any, is added to the context alongside).
    private async Task<ListingImportRow> ImportRowAsync(
        ImportCsvRow csvRow, ImportRowParser parser, ImportGeolocator geolocator, Guid agencyId, Guid jobId, Guid photoUploader, CancellationToken cancellationToken)
    {
        try
        {
            var parsed = await parser.ParseAsync(csvRow, cancellationToken);
            if (parsed.Plan is not { } plan)
            {
                return ListingImportRow.Failed(jobId, csvRow.RowNumber, parsed.ExternalRef, parsed.Problems);
            }

            var command = plan.Command;
            var where = await geolocator.LocateAsync(plan.Location, command.StreetAddress, command.BuildingNumber, plan.Latitude, plan.Longitude, cancellationToken);
            var written = await writer.WriteAsync(plan, agencyId, photoUploader, where.Latitude, where.Longitude, cancellationToken);

            var warnings = plan.Warnings
                .Concat(where.Warning is { } warning ? [warning] : [])
                .Concat(written.Problems.Where(p => p.Severity == ListingImportProblemSeverity.Warning))
                .ToList();

            return written.Listing is { } listing
                ? ListingImportRow.Imported(jobId, csvRow.RowNumber, plan.ExternalRef, written.Created, listing.Id, warnings)
                : ListingImportRow.Failed(jobId, csvRow.RowNumber, plan.ExternalRef, [.. written.Problems.Where(p => p.Severity == ListingImportProblemSeverity.Error), .. warnings]);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Import {JobId}: row {Row} failed.", jobId, csvRow.RowNumber);
            dbContext.DiscardChanges();
            return ListingImportRow.Failed(jobId, csvRow.RowNumber, csvRow.Get(ImportColumns.ExternalRef), [RowFailed()]);
        }
    }

    private static ListingImportProblem RowFailed() =>
        ImportProblems.Error(null, ErrorCodes.ImportRowFailed, "Something went wrong saving this row; try importing it again.");

    private async Task<byte[]?> ReadFileAsync(ListingImportJob job, CancellationToken cancellationToken)
    {
        if (job.FileBlobName is not { } blobName)
        {
            return null;
        }

        await using var stream = await blobStorageService.OpenPrivateAsync(blobName, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private async Task NotifyAsync(ListingImportJob job, CancellationToken cancellationToken)
    {
        try
        {
            var to = job.UploadedByUserId is { } userId
                ? await dbContext.Users.AsNoTracking().Where(u => u.Id == userId && u.EmailConfirmed).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken)
                : null;
            if (string.IsNullOrEmpty(to))
            {
                return;
            }

            var agencyName = await dbContext.Agencies.AsNoTracking().Where(a => a.Id == job.AgencyId).Select(a => a.Name).FirstAsync(cancellationToken);
            var url = $"{appOptions.WebBaseUrl.TrimEnd('/')}/account/agencies/{job.AgencyId}/import/{job.Id}";
            await emailSender.SendAsync(ListingImportEmails.Finished(to, agencyName, job, url), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Import {JobId}: the email to the uploader couldn't be sent.", job.Id);
        }
    }

    private async Task DeleteOldFilesAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - ListingImportJob.KeepFileFor;
        var old = await dbContext.ListingImportJobs
            .Where(j => j.FileBlobName != null && j.CreatedAt < cutoff
                && j.Status != ListingImportStatus.Pending && j.Status != ListingImportStatus.Processing)
            .Take(100)
            .ToListAsync(cancellationToken);
        foreach (var job in old)
        {
            await blobStorageService.DeletePrivateAsync(job.FileBlobName!, cancellationToken);
            job.MarkFileDeleted(timeProvider.GetUtcNow());
        }

        if (old.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task FailStaleAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - StaleAfter;
        var stale = await dbContext.ListingImportJobs
            .Where(j => j.Status == ListingImportStatus.Processing && j.StartedAt < cutoff)
            .ToListAsync(cancellationToken);
        foreach (var job in stale)
        {
            logger.LogWarning("Import {JobId} was still processing after {Hours} hours; marking it failed.", job.Id, StaleAfter.TotalHours);
            job.Fail(ErrorCodes.ImportWorkerFailed, timeProvider.GetUtcNow());
        }

        if (stale.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
