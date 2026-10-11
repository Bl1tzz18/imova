using Imova.Domain.Common;

namespace Imova.Domain.Agencies;

public enum ListingImportRowOutcome
{
    Created = 1,
    Updated = 2,
    Failed = 3,
}

public enum ListingImportProblemSeverity
{
    // The row wasn't imported.
    Error = 1,
    // The row was imported, with something the agency should know (e.g. imported under the uploader
    // because agent_email isn't a member).
    Warning = 2,
}

// What was wrong with one value (or the row): Column is the CSV column (null for the row as a
// whole), Code an error code the web app translates (the same codes as the listing form's, plus
// import.*), Params its values, Message the English text for developers.
public sealed record ListingImportProblem(
    ListingImportProblemSeverity Severity,
    string? Column,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Params = null);

// What happened to one data row of an import: created or updated a listing, or failed — with its
// problems (errors and warnings). The error report is built from these rows.
public sealed class ListingImportRow : Entity
{
    public const int MaxExternalRefLength = 100;

    // For EF Core materialization only.
    private ListingImportRow()
        : base(Guid.Empty)
    {
        Problems = [];
    }

    private ListingImportRow(Guid id, Guid jobId, int rowNumber, string? externalRef, ListingImportRowOutcome outcome, Guid? listingId, IReadOnlyList<ListingImportProblem> problems)
        : base(id)
    {
        JobId = jobId;
        RowNumber = rowNumber;
        ExternalRef = externalRef;
        Outcome = outcome;
        ListingId = listingId;
        Problems = problems;
    }

    public Guid JobId { get; private set; }

    // The line in the file, counting the header as line 1 — what a spreadsheet shows next to it.
    public int RowNumber { get; private set; }

    // As read from the row (trimmed, cut to MaxExternalRefLength); null when the cell was empty.
    public string? ExternalRef { get; private set; }

    public ListingImportRowOutcome Outcome { get; private set; }

    // The listing created or updated; null for a failed row.
    public Guid? ListingId { get; private set; }

    public IReadOnlyList<ListingImportProblem> Problems { get; private set; }

    public bool HasWarnings => Problems.Any(p => p.Severity == ListingImportProblemSeverity.Warning);

    public static ListingImportRow Imported(
        Guid jobId, int rowNumber, string externalRef, bool created, Guid listingId, IReadOnlyList<ListingImportProblem>? warnings = null)
    {
        var problems = warnings ?? [];
        if (problems.Any(p => p.Severity == ListingImportProblemSeverity.Error))
        {
            throw new ArgumentException("An imported row can only carry warnings.", nameof(warnings));
        }

        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("ListingId is required.", nameof(listingId));
        }

        return new ListingImportRow(
            Guid.NewGuid(), jobId, rowNumber, Ref(externalRef), created ? ListingImportRowOutcome.Created : ListingImportRowOutcome.Updated, listingId, problems);
    }

    public static ListingImportRow Failed(Guid jobId, int rowNumber, string? externalRef, IReadOnlyList<ListingImportProblem> problems)
    {
        if (!problems.Any(p => p.Severity == ListingImportProblemSeverity.Error))
        {
            throw new ArgumentException("A failed row needs at least one error.", nameof(problems));
        }

        return new ListingImportRow(Guid.NewGuid(), jobId, rowNumber, Ref(externalRef), ListingImportRowOutcome.Failed, null, problems);
    }

    private static string? Ref(string? externalRef)
    {
        var value = externalRef?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.Length > MaxExternalRefLength ? value[..MaxExternalRefLength] : value;
    }
}
