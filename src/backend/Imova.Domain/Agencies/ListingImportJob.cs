using Imova.Domain.Common;

namespace Imova.Domain.Agencies;

public enum ListingImportStatus
{
    // Uploaded, waiting for the Worker.
    Pending = 1,
    Processing = 2,
    // Every row was tried (some may have failed — see the counts and the rows).
    Completed = 3,
    // The file as a whole couldn't be processed (unreadable, no header, too many rows, …).
    Failed = 4,
}

// One CSV upload by an agency's Owner/Admin: the file waits in private storage, the Worker creates or
// updates a listing per row (matched by ExternalRef), and the job keeps the counts for the result
// page. One row per data line is kept in ListingImportRows (what happened, and why a row failed).
// At most one job per agency is Pending or Processing at a time (the upload refuses a second).
public sealed class ListingImportJob : Entity
{
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxRows = 2_000;
    public const int MaxFileNameLength = 255;

    // The uploaded file is deleted this long after the upload; the rows and counts stay.
    public static readonly TimeSpan KeepFileFor = TimeSpan.FromDays(30);

    // For EF Core materialization only.
    private ListingImportJob()
        : base(Guid.Empty)
    {
        FileName = null!;
    }

    private ListingImportJob(Guid id, Guid agencyId, Guid uploadedByUserId, string fileName, string fileBlobName, long fileSizeBytes, DateTimeOffset now)
        : base(id)
    {
        AgencyId = agencyId;
        UploadedByUserId = uploadedByUserId;
        FileName = fileName;
        FileBlobName = fileBlobName;
        FileSizeBytes = fileSizeBytes;
        Status = ListingImportStatus.Pending;
        CreatedAt = now;
    }

    public Guid AgencyId { get; private set; }

    // Null once that account is deleted (the job stays in the agency's history).
    public Guid? UploadedByUserId { get; private set; }

    // As uploaded, shown in the history.
    public string FileName { get; private set; }

    // Where the file waits in private storage; null once it's deleted (KeepFileFor).
    public string? FileBlobName { get; private set; }

    public long FileSizeBytes { get; private set; }

    public ListingImportStatus Status { get; private set; }

    // Data rows in the file (the header not counted); known once processing starts.
    public int TotalRows { get; private set; }

    public int CreatedCount { get; private set; }

    public int UpdatedCount { get; private set; }

    public int FailedCount { get; private set; }

    // Rows imported with a warning (e.g. agent_email not a member — imported under the uploader).
    public int WarningCount { get; private set; }

    // The agency's listings with an ExternalRef that this file didn't mention — information only,
    // nothing is done to them.
    public int NotInFileCount { get; private set; }

    // Why a Failed job failed as a whole: an error code the web app translates (import.*).
    public string? FailureCode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public DateTimeOffset? FileDeletedAt { get; private set; }

    // Waiting or running — blocks another upload for the same agency.
    public bool IsActive => Status is ListingImportStatus.Pending or ListingImportStatus.Processing;

    public static ListingImportJob Create(
        Guid agencyId, Guid uploadedByUserId, string fileName, string fileBlobName, long fileSizeBytes, DateTimeOffset now)
    {
        if (agencyId == Guid.Empty)
        {
            throw new ArgumentException("AgencyId is required.", nameof(agencyId));
        }

        if (uploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("UploadedByUserId is required.", nameof(uploadedByUserId));
        }

        if (string.IsNullOrWhiteSpace(fileBlobName))
        {
            throw new ArgumentException("FileBlobName is required.", nameof(fileBlobName));
        }

        if (fileSizeBytes is <= 0 or > MaxFileBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), $"The file must be 1 to {MaxFileBytes} bytes.");
        }

        return new ListingImportJob(Guid.NewGuid(), agencyId, uploadedByUserId, CleanFileName(fileName), fileBlobName, fileSizeBytes, now);
    }

    public void Start(int totalRows, DateTimeOffset now)
    {
        if (Status != ListingImportStatus.Pending)
        {
            throw new InvalidOperationException($"Only a pending import can start (this one is {Status}).");
        }

        if (totalRows is < 0 or > MaxRows)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRows), $"An import has 0 to {MaxRows} rows.");
        }

        Status = ListingImportStatus.Processing;
        TotalRows = totalRows;
        StartedAt = now;
    }

    public void Complete(int created, int updated, int failed, int warnings, int notInFile, DateTimeOffset now)
    {
        if (Status != ListingImportStatus.Processing)
        {
            throw new InvalidOperationException($"Only a running import can complete (this one is {Status}).");
        }

        if (created < 0 || updated < 0 || failed < 0 || warnings < 0 || notInFile < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(created), "Counts can't be negative.");
        }

        if (created + updated + failed != TotalRows)
        {
            throw new InvalidOperationException($"Every row ends created, updated or failed ({created} + {updated} + {failed} ≠ {TotalRows}).");
        }

        Status = ListingImportStatus.Completed;
        CreatedCount = created;
        UpdatedCount = updated;
        FailedCount = failed;
        WarningCount = warnings;
        NotInFileCount = notInFile;
        FinishedAt = now;
    }

    // The file as a whole couldn't be processed — from Pending (it couldn't even be read) or
    // Processing (e.g. the Worker gave up). Rows already written stay.
    public void Fail(string failureCode, DateTimeOffset now)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException($"A finished import can't fail (this one is {Status}).");
        }

        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A failure code is required.", nameof(failureCode));
        }

        Status = ListingImportStatus.Failed;
        FailureCode = failureCode;
        FinishedAt = now;
    }

    public void MarkFileDeleted(DateTimeOffset now)
    {
        FileBlobName = null;
        FileDeletedAt = now;
    }

    // Only the name (no folders a browser may send), never empty, at most MaxFileNameLength.
    private static string CleanFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/')).Trim();
        if (name.Length == 0)
        {
            name = "import.csv";
        }

        return name.Length > MaxFileNameLength ? name[..MaxFileNameLength] : name;
    }
}
