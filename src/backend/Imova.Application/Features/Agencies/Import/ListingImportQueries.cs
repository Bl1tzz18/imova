using System.Globalization;
using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

// POST /api/v1/agencies/{id}/import — a CSV for the Worker. Checked at once so the uploader hears
// about a wrong file straight away: size, then the header (encoding, separator, known columns, row
// count — the same ImportCsvReader the Worker uses). Rows are checked by the Worker. Null → 404.
public sealed record UploadListingImportCommand(Guid AgencyId, Guid UserId, bool IsAdmin, string FileName, byte[] Content)
    : IRequest<ListingImportJobDto?>;

public class UploadListingImportHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IDatabaseErrors databaseErrors,
    TimeProvider timeProvider) : IRequestHandler<UploadListingImportCommand, ListingImportJobDto?>
{
    public async Task<ListingImportJobDto?> Handle(UploadListingImportCommand request, CancellationToken cancellationToken)
    {
        var agency = await ImportAccess.EnsureCanUploadAsync(dbContext, request.AgencyId, request.UserId, request.IsAdmin, cancellationToken);
        if (agency is null)
        {
            return null;
        }

        if (request.Content.Length == 0)
        {
            throw Invalid("Choose a CSV file.", ErrorCodes.ImportFileRequired);
        }

        if (request.Content.Length > ListingImportJob.MaxFileBytes)
        {
            throw Invalid(
                $"The file is larger than {ListingImportJob.MaxFileBytes / (1024 * 1024)} MB.",
                ErrorCodes.ImportFileTooLarge,
                CodedFailure.Params(("maxMb", ListingImportJob.MaxFileBytes / (1024 * 1024))));
        }

        var (_, fileError) = ImportCsvReader.Read(request.Content);
        if (fileError is not null)
        {
            throw Invalid(fileError.Message, fileError.Code, fileError.Params?.ToDictionary(p => p.Key, p => (object)p.Value));
        }

        if (await dbContext.ListingImportJobs.AnyAsync(
                j => j.AgencyId == agency.Id && (j.Status == ListingImportStatus.Pending || j.Status == ListingImportStatus.Processing),
                cancellationToken))
        {
            throw AlreadyRunning();
        }

        var blobName = blobStorageService.GenerateImportFileBlobName(agency.Id);
        using (var stream = new MemoryStream(request.Content, writable: false))
        {
            await blobStorageService.UploadPrivateAsync(blobName, stream, "text/csv", cancellationToken);
        }

        var job = ListingImportJob.Create(agency.Id, request.UserId, request.FileName, blobName, request.Content.Length, timeProvider.GetUtcNow());
        dbContext.ListingImportJobs.Add(job);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (databaseErrors.IsUniqueViolation(ex, ImportAccess.ActiveJobIndex))
        {
            await blobStorageService.DeletePrivateAsync(blobName, cancellationToken);
            throw AlreadyRunning();
        }

        return (await ListingImportDtos.JobsAsync(dbContext, [job], cancellationToken))[0];
    }

    private static ConflictException AlreadyRunning() =>
        new("An import for this agency is already waiting or running.", ErrorCodes.ImportAlreadyRunning);

    private static ValidationException Invalid(string message, string code, IReadOnlyDictionary<string, object>? parameters = null) =>
        new([CodedFailure.Of("File", message, code, parameters)]);
}

// GET /api/v1/agencies/{id}/import — the agency's imports, newest first (the latest 20). Owners,
// Admins, site admins; null → 404.
public sealed record GetListingImportsQuery(Guid AgencyId, Guid UserId, bool IsAdmin) : IRequest<IReadOnlyList<ListingImportJobDto>?>;

public class GetListingImportsHandler(IApplicationDbContext dbContext) : IRequestHandler<GetListingImportsQuery, IReadOnlyList<ListingImportJobDto>?>
{
    public const int Latest = 20;

    public async Task<IReadOnlyList<ListingImportJobDto>?> Handle(GetListingImportsQuery request, CancellationToken cancellationToken)
    {
        if (!await ImportAccess.CanPrepareAsync(dbContext, request.AgencyId, request.UserId, request.IsAdmin, cancellationToken))
        {
            return null;
        }

        var jobs = await dbContext.ListingImportJobs.AsNoTracking()
            .Where(j => j.AgencyId == request.AgencyId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(Latest)
            .ToListAsync(cancellationToken);
        return await ListingImportDtos.JobsAsync(dbContext, jobs, cancellationToken);
    }
}

// GET /api/v1/agencies/{id}/import/{jobId} — one import (polled while it runs). Null → 404.
public sealed record GetListingImportQuery(Guid AgencyId, Guid JobId, Guid UserId, bool IsAdmin) : IRequest<ListingImportJobDto?>;

public class GetListingImportHandler(IApplicationDbContext dbContext) : IRequestHandler<GetListingImportQuery, ListingImportJobDto?>
{
    public async Task<ListingImportJobDto?> Handle(GetListingImportQuery request, CancellationToken cancellationToken)
    {
        if (!await ImportAccess.CanPrepareAsync(dbContext, request.AgencyId, request.UserId, request.IsAdmin, cancellationToken))
        {
            return null;
        }

        var job = await dbContext.ListingImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == request.JobId && j.AgencyId == request.AgencyId, cancellationToken);
        return job is null ? null : (await ListingImportDtos.JobsAsync(dbContext, [job], cancellationToken))[0];
    }
}

// GET /api/v1/agencies/{id}/import/{jobId}/rows?problems=&page=&pageSize= — the rows in file order;
// problems=true: only those with errors or warnings (what the report lists). Null → 404.
public sealed record GetListingImportRowsQuery(Guid AgencyId, Guid JobId, Guid UserId, bool IsAdmin, bool OnlyProblems, int Page = 1, int PageSize = 100)
    : IRequest<ListingImportRowsPageDto?>;

public class GetListingImportRowsValidator : AbstractValidator<GetListingImportRowsQuery>
{
    public GetListingImportRowsValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, ListingImportJob.MaxRows);
    }
}

public class GetListingImportRowsHandler(IApplicationDbContext dbContext) : IRequestHandler<GetListingImportRowsQuery, ListingImportRowsPageDto?>
{
    public async Task<ListingImportRowsPageDto?> Handle(GetListingImportRowsQuery request, CancellationToken cancellationToken)
    {
        if (!await ImportAccess.CanPrepareAsync(dbContext, request.AgencyId, request.UserId, request.IsAdmin, cancellationToken)
            || !await dbContext.ListingImportJobs.AnyAsync(j => j.Id == request.JobId && j.AgencyId == request.AgencyId, cancellationToken))
        {
            return null;
        }

        var rows = await dbContext.ListingImportRows.AsNoTracking()
            .Where(r => r.JobId == request.JobId)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);

        // Problems are JSON: filtered here (at most MaxRows rows per job).
        var selected = request.OnlyProblems ? rows.Where(r => r.Problems.Count > 0).ToList() : rows;
        var page = selected.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(ListingImportDtos.Row).ToList();
        return new ListingImportRowsPageDto(page, request.Page, request.PageSize, selected.Count);
    }
}

public static class ListingImportDtos
{
    public static async Task<IReadOnlyList<ListingImportJobDto>> JobsAsync(
        IApplicationDbContext dbContext, IReadOnlyList<ListingImportJob> jobs, CancellationToken cancellationToken)
    {
        var jobIds = jobs.Select(j => j.Id).ToList();
        var processed = await dbContext.ListingImportRows.AsNoTracking()
            .Where(r => jobIds.Contains(r.JobId))
            .GroupBy(r => r.JobId)
            .Select(g => new { JobId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.JobId, g => g.Count, cancellationToken);

        var uploaderIds = jobs.Select(j => j.UploadedByUserId).OfType<Guid>().Distinct().ToList();
        var names = await dbContext.Users.AsNoTracking()
            .Where(u => uploaderIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.DisplayName) ? u.Email : u.DisplayName, cancellationToken);

        return jobs.Select(j => new ListingImportJobDto(
                j.Id,
                j.FileName,
                j.Status.ToString(),
                j.TotalRows,
                processed.GetValueOrDefault(j.Id),
                j.CreatedCount,
                j.UpdatedCount,
                j.FailedCount,
                j.WarningCount,
                j.NotInFileCount,
                j.FailureCode,
                j.UploadedByUserId is { } id ? names.GetValueOrDefault(id) : null,
                j.CreatedAt,
                j.StartedAt,
                j.FinishedAt,
                j.FileBlobName is not null))
            .ToList();
    }

    public static ListingImportRowDto Row(ListingImportRow row) =>
        new(
            row.RowNumber,
            row.ExternalRef,
            row.Outcome.ToString(),
            row.ListingId,
            row.Problems.Select(p => new ListingImportProblemDto(p.Severity.ToString(), p.Column, p.Code, p.Params, p.Message)).ToList());

    // The report as a CSV (GET …/import/{jobId}/report.csv): one line per problem, codes + English
    // text — for scripts; the import page shows the same in the viewer's language.
    public static byte[] ReportCsv(IEnumerable<ListingImportRowDto> rows)
    {
        var buffer = new StringWriter(CultureInfo.InvariantCulture);
        using (var csv = new CsvHelper.CsvWriter(buffer, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ImportTemplate.Delimiter }))
        {
            foreach (var header in new[] { "row", "external_ref", "outcome", "severity", "column", "code", "message" })
            {
                csv.WriteField(header);
            }

            csv.NextRecord();
            foreach (var row in rows)
            {
                foreach (var problem in row.Problems)
                {
                    csv.WriteField(row.RowNumber);
                    csv.WriteField(row.ExternalRef ?? string.Empty);
                    csv.WriteField(row.Outcome);
                    csv.WriteField(problem.Severity);
                    csv.WriteField(problem.Column ?? string.Empty);
                    csv.WriteField(problem.Code);
                    csv.WriteField(problem.Message);
                    csv.NextRecord();
                }
            }
        }

        return [0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes(buffer.ToString())];
    }
}
