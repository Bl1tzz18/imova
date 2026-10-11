using Imova.Domain.Agencies;

namespace Imova.UnitTests.Agencies;

public class ListingImportJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private static ListingImportJob NewJob(string fileName = "anunturi.csv", long size = 1_000) =>
        ListingImportJob.Create(Guid.NewGuid(), Guid.NewGuid(), fileName, "imports/a/b.csv", size, Now);

    [Fact]
    public void Create_StartsPending_WithOnlyTheFilesName()
    {
        var job = NewJob(@"C:\fakepath\Export Casa Ta.csv");

        Assert.Equal(ListingImportStatus.Pending, job.Status);
        Assert.True(job.IsActive);
        Assert.Equal("Export Casa Ta.csv", job.FileName);
        Assert.Equal("import.csv", NewJob("  ").FileName);
        Assert.Equal(ListingImportJob.MaxFileNameLength, NewJob(new string('a', 300) + ".csv").FileName.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ListingImportJob.MaxFileBytes + 1)]
    public void Create_RefusesAnEmptyOrTooLargeFile(long size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NewJob(size: size));

    [Fact]
    public void Start_ThenComplete_KeepsTheCounts()
    {
        var job = NewJob();
        job.Start(totalRows: 5, Now.AddMinutes(1));
        job.Complete(created: 3, updated: 1, failed: 1, warnings: 2, notInFile: 4, Now.AddMinutes(2));

        Assert.Equal(ListingImportStatus.Completed, job.Status);
        Assert.False(job.IsActive);
        Assert.Equal((5, 3, 1, 1, 2, 4), (job.TotalRows, job.CreatedCount, job.UpdatedCount, job.FailedCount, job.WarningCount, job.NotInFileCount));
        Assert.Equal(Now.AddMinutes(1), job.StartedAt);
        Assert.Equal(Now.AddMinutes(2), job.FinishedAt);
    }

    [Fact]
    public void Complete_NeedsEveryRowAccountedFor()
    {
        var job = NewJob();
        job.Start(totalRows: 5, Now);

        Assert.Throws<InvalidOperationException>(() => job.Complete(3, 1, 0, 0, 0, Now));
    }

    [Fact]
    public void TheLifecycleOnlyMovesForward()
    {
        var job = NewJob();
        Assert.Throws<InvalidOperationException>(() => job.Complete(0, 0, 0, 0, 0, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.Start(ListingImportJob.MaxRows + 1, Now));

        job.Start(0, Now);
        Assert.Throws<InvalidOperationException>(() => job.Start(0, Now));

        job.Fail("import.workerFailed", Now);
        Assert.Equal(ListingImportStatus.Failed, job.Status);
        Assert.Equal("import.workerFailed", job.FailureCode);
        Assert.Throws<InvalidOperationException>(() => job.Fail("import.workerFailed", Now));
    }

    [Fact]
    public void APendingJob_CanFail_WhenItsFileCantBeRead()
    {
        var job = NewJob();
        job.Fail("import.fileUnreadable", Now);

        Assert.Equal(ListingImportStatus.Failed, job.Status);
        Assert.Null(job.StartedAt);
    }

    [Fact]
    public void MarkFileDeleted_ForgetsWhereTheFileWas()
    {
        var job = NewJob();
        job.MarkFileDeleted(Now);

        Assert.Null(job.FileBlobName);
        Assert.Equal(Now, job.FileDeletedAt);
    }

    [Fact]
    public void ARow_IsImportedWithWarnings_OrFailedWithErrors()
    {
        var jobId = Guid.NewGuid();
        var warning = new ListingImportProblem(ListingImportProblemSeverity.Warning, "agent_email", "import.agentNotMember", "Not a member.");
        var error = new ListingImportProblem(ListingImportProblemSeverity.Error, "price", "import.notANumber", "Not a number.");

        var imported = ListingImportRow.Imported(jobId, 2, "  AP-1  ", created: true, Guid.NewGuid(), [warning]);
        Assert.Equal(ListingImportRowOutcome.Created, imported.Outcome);
        Assert.Equal("AP-1", imported.ExternalRef);
        Assert.True(imported.HasWarnings);

        var failed = ListingImportRow.Failed(jobId, 3, "", [error, warning]);
        Assert.Equal(ListingImportRowOutcome.Failed, failed.Outcome);
        Assert.Null(failed.ExternalRef);
        Assert.Null(failed.ListingId);

        Assert.Throws<ArgumentException>(() => ListingImportRow.Imported(jobId, 4, "AP-2", created: false, Guid.NewGuid(), [error]));
        Assert.Throws<ArgumentException>(() => ListingImportRow.Failed(jobId, 5, "AP-3", [warning]));
        Assert.Equal(ListingImportRow.MaxExternalRefLength, ListingImportRow.Failed(jobId, 6, new string('x', 150), [error]).ExternalRef!.Length);
    }
}
