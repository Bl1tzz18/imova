namespace Imova.Contracts.Agencies;

// One CSV import of an agency (POST/GET /api/v1/agencies/{id}/import[/{jobId}]).
// Status: Pending | Processing | Completed | Failed. ProcessedRows: rows done so far (the progress
// while Processing). NotInFileCount: the agency's live listings with an external_ref this file
// didn't mention — information only, nothing was done to them. FailureCode (import.*): why a Failed
// import failed as a whole. FileAvailable: the uploaded file is still kept (30 days).
public record ListingImportJobDto(
    Guid Id,
    string FileName,
    string Status,
    int TotalRows,
    int ProcessedRows,
    int CreatedCount,
    int UpdatedCount,
    int FailedCount,
    int WarningCount,
    int NotInFileCount,
    string? FailureCode,
    string? UploadedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    bool FileAvailable);

// What was wrong (Severity: Error | Warning) with one value of a row (Column = the CSV column, null
// for the row as a whole). Code: an error code the web app translates; Message: English, for developers.
public record ListingImportProblemDto(
    string Severity,
    string? Column,
    string Code,
    IReadOnlyDictionary<string, string>? Params,
    string Message);

// One row of an import (GET …/import/{jobId}/rows). Outcome: Created | Updated | Failed. ListingId:
// the listing created or updated.
public record ListingImportRowDto(
    int RowNumber,
    string? ExternalRef,
    string Outcome,
    Guid? ListingId,
    IReadOnlyList<ListingImportProblemDto> Problems);

public record ListingImportRowsPageDto(IReadOnlyList<ListingImportRowDto> Items, int Page, int PageSize, int TotalCount);
