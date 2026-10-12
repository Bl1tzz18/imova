using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Agencies.Import;
using Imova.Application.Features.Media.Sizes;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.Infrastructure.Amenities;
using Imova.Infrastructure.Proximities;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ValidationException = FluentValidation.ValidationException;

namespace Imova.UnitTests.Agencies;

// The import end to end without a server: the upload's checks, the Worker job turning every row into
// a listing (or a reported failure), re-imports by external_ref, a site admin importing for an agency
// (only site admins may — IMOVA imports by hand), no email, the reads, and the job's housekeeping.
public class ListingImportsJobTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly FailingListingSave _failingSave = new();
    private readonly ImovaDbContext _db;
    private readonly ManualTimeProvider _clock = new(Start);
    private readonly FakeBlobStorageService _blobs = new();
    private readonly FakeImportImageFetcher _fetcher = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Agency _agency;

    public ListingImportsJobTests()
    {
        _db = TestDbContextFactory.Create(Guid.NewGuid().ToString(), _failingSave);
        new ImportTestPlaces().AddTo(_db);
        _db.Amenities.AddRange(AmenityConfiguration.Seed);
        _db.Proximities.AddRange(ProximityConfiguration.Seed);
        AddUser(_owner);
        _agency = ListingTestData.AddAgency(_db, _owner, "Casa Ta");
        _agency.Verify(Start);
        _db.SaveChanges();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private void AddUser(Guid id)
    {
        var user = ListingTestData.AddUser(_db, id);
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
    }

    private ListingImports Job() =>
        new(
            _db,
            _blobs,
            new FakeGeocodingService(),
            new ImportListingWriter(
                _db,
                new FakeExchangeRateProvider(),
                new ImportPhotoSync(_db, _blobs, _fetcher, new PhotoSizeGenerator(_blobs, new FakePhotoResizer()), NullLogger<ImportPhotoSync>.Instance)),
            _clock,
            NullLogger<ListingImports>.Instance);

    // Only site admins import: by default the upload is by the Owner acting as a site admin (a member,
    // so the Owner stays the author).
    private Task<Contracts.Agencies.ListingImportJobDto?> UploadAsync(byte[] content, Guid? userId = null, bool isAdmin = true) =>
        new UploadListingImportHandler(_db, _blobs, new FakeDatabaseErrors(), _clock).Handle(
            new UploadListingImportCommand(_agency.Id, userId ?? _owner, isAdmin, "anunturi.csv", content), CancellationToken.None);

    // A semicolon CSV of the given types' example rows (with changes), headed by every column they use.
    private static byte[] Csv(params (PropertyType Type, (string Column, string Value)[] Changes)[] rows)
    {
        var columns = rows.SelectMany(r => ImportColumns.For(r.Type).Select(c => c.Name)).Distinct().ToList();
        var text = new StringWriter(CultureInfo.InvariantCulture);
        using (var csv = new CsvWriter(text, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" }))
        {
            columns.ForEach(c => csv.WriteField(c));
            csv.NextRecord();
            foreach (var (type, changes) in rows)
            {
                var values = new Dictionary<string, string>(ImportExamples.For(type));
                foreach (var (column, value) in changes)
                {
                    values[column] = value;
                }

                columns.ForEach(c => csv.WriteField(values.GetValueOrDefault(c, string.Empty)));
                csv.NextRecord();
            }
        }

        return Encoding.UTF8.GetBytes(text.ToString());
    }

    private static (PropertyType, (string, string)[]) Row(PropertyType type, params (string, string)[] changes) => (type, changes);

    private Task<Listing> ListingAsync(string externalRef) =>
        _db.Listings.AsNoTracking().SingleAsync(l => l.AgencyId == _agency.Id && l.ExternalRef == externalRef);

    private async Task<ListingImportJob> RunAsync(Guid jobId)
    {
        Assert.Equal(1, await Job().RunAsync(CancellationToken.None));
        _db.ChangeTracker.Clear();
        return await _db.ListingImportJobs.SingleAsync(j => j.Id == jobId);
    }

    [Fact]
    public async Task AnUpload_IsImportedByTheWorker_RowByRow_WithTheFailuresReported()
    {
        var upload = (await UploadAsync(Csv(
            Row(PropertyType.Apartment),
            Row(PropertyType.Garage),
            Row(PropertyType.Room, ("price", "trei mii")))))!;
        Assert.Equal("Pending", upload.Status);
        Assert.True(upload.FileAvailable);

        var job = await RunAsync(upload.Id);

        Assert.Equal(ListingImportStatus.Completed, job.Status);
        Assert.Equal((3, 2, 0, 1), (job.TotalRows, job.CreatedCount, job.UpdatedCount, job.FailedCount));
        var apartment = await ListingAsync("AP-1024");
        Assert.Equal((ListingStatus.Active, 78000m), (apartment.Status, apartment.Price.Amount));
        Assert.NotNull(apartment.PublishedAt);
        Assert.Equal(3, await _db.Photos.CountAsync(p => p.ListingId == apartment.Id && p.SourceUrl != null));
        Assert.Equal(_owner, (await _db.Publishers.SingleAsync(p => p.Id == apartment.PublisherId)).UserId);
        Assert.Equal(ListingStatus.Active, (await ListingAsync("GR-3")).Status);

        var rows = await _db.ListingImportRows.Where(r => r.JobId == job.Id).OrderBy(r => r.RowNumber).ToListAsync();
        Assert.Equal([2, 3, 4], rows.Select(r => r.RowNumber));
        Assert.Equal(apartment.Id, rows[0].ListingId);
        var failure = Assert.Single(rows[2].Problems);
        Assert.Equal(("price", ErrorCodes.ImportNotANumber), (failure.Column, failure.Code));

        Assert.Equal(0, await Job().RunAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AReImport_UpdatesByExternalRef_DeactivatesInactiveRows_AndCountsWhatWasntInTheFile()
    {
        await RunAsync((await UploadAsync(Csv(Row(PropertyType.Apartment), Row(PropertyType.Garage), Row(PropertyType.Room))))!.Id);
        var apartment = await ListingAsync("AP-1024");
        _fetcher.Downloaded.Clear();

        var job = await RunAsync((await UploadAsync(Csv(
            Row(PropertyType.Apartment, ("price", "75 000")),
            Row(PropertyType.Garage, ("status", "inactive")))))!.Id);

        Assert.Equal((2, 0, 2, 0, 1), (job.TotalRows, job.CreatedCount, job.UpdatedCount, job.FailedCount, job.NotInFileCount));
        Assert.Empty(_fetcher.Downloaded);
        Assert.Equal(apartment.Id, (await ListingAsync("AP-1024")).Id);
        Assert.Equal(75000m, (await ListingAsync("AP-1024")).Price.Amount);
        Assert.Single(await _db.ListingPriceChanges.Where(c => c.ListingId == apartment.Id).ToListAsync());
        Assert.Equal(ListingStatus.Archived, (await ListingAsync("GR-3")).Status);
        Assert.Equal(ListingStatus.Active, (await ListingAsync("CM-12")).Status);
        Assert.Equal(3, await _db.Listings.CountAsync(l => l.AgencyId == _agency.Id));
    }

    [Fact]
    public async Task ASiteAdmin_ImportsForAVerifiedAgencyTheyDontBelongTo_UnderItsOwner()
    {
        var siteAdmin = Guid.NewGuid();
        AddUser(siteAdmin);
        await _db.SaveChangesAsync();

        var upload = (await UploadAsync(Csv(Row(PropertyType.Apartment)), siteAdmin, isAdmin: true))!;
        var job = await RunAsync(upload.Id);

        Assert.Equal((1, siteAdmin), (job.CreatedCount, job.UploadedByUserId!.Value));
        var listing = await ListingAsync("AP-1024");
        Assert.Equal(_owner, (await _db.Publishers.SingleAsync(p => p.Id == listing.PublisherId)).UserId);
        Assert.Equal(_agency.Id, listing.AgencyId);
        Assert.DoesNotContain(siteAdmin, (await _db.Agencies.Include(a => a.Members).SingleAsync()).Members.Select(m => m.UserId));
    }

    [Fact]
    public async Task OnlyASiteAdmin_MayUpload_ForAVerifiedActiveAgency()
    {
        var agent = Guid.NewGuid();
        AddUser(agent);
        var agency = await _db.Agencies.Include(a => a.Members).SingleAsync();
        agency.AddMember(agent, AgencyRole.Agent, Start);
        await _db.SaveChangesAsync();
        var file = Csv(Row(PropertyType.Garage));

        // The agency's own Owner, an Agent, a stranger: importing is IMOVA's service, done by a site admin.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => UploadAsync(file, _owner, isAdmin: false));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => UploadAsync(file, agent, isAdmin: false));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => UploadAsync(file, Guid.NewGuid(), isAdmin: false));
        Assert.Null(await new UploadListingImportHandler(_db, _blobs, new FakeDatabaseErrors(), _clock).Handle(
            new UploadListingImportCommand(Guid.NewGuid(), _owner, true, "x.csv", file), CancellationToken.None));

        agency.Unverify(Start);
        await _db.SaveChangesAsync();
        Assert.Equal(ErrorCodes.ImportAgencyNotVerified, (await Assert.ThrowsAsync<ForbiddenAccessException>(() => UploadAsync(file))).Code);

        agency.Verify(Start);
        agency.Deactivate(Start);
        await _db.SaveChangesAsync();
        Assert.Equal(ErrorCodes.ImportAgencyInactive, (await Assert.ThrowsAsync<ForbiddenAccessException>(() => UploadAsync(file))).Code);
    }

    [Fact]
    public async Task AWrongFile_IsRefusedAtOnce_AndOnlyOneImportRunsAtATime()
    {
        Assert.Equal(ErrorCodes.ImportFileRequired, (await Assert.ThrowsAsync<ValidationException>(() => UploadAsync([]))).Errors.Single().ErrorCode);
        Assert.Equal(
            ErrorCodes.ImportUnknownColumns,
            (await Assert.ThrowsAsync<ValidationException>(() => UploadAsync("external_ref;property_type;rooom\nA;Apartment;2\n"u8.ToArray()))).Errors.Single().ErrorCode);
        Assert.Equal(
            ErrorCodes.ImportFileTooLarge,
            (await Assert.ThrowsAsync<ValidationException>(() => UploadAsync(new byte[ListingImportJob.MaxFileBytes + 1]))).Errors.Single().ErrorCode);
        Assert.Empty(_blobs.PrivateContent);

        await UploadAsync(Csv(Row(PropertyType.Garage)));
        Assert.Equal(ErrorCodes.ImportAlreadyRunning, (await Assert.ThrowsAsync<ConflictException>(() => UploadAsync(Csv(Row(PropertyType.Garage))))).Code);
    }

    [Fact]
    public async Task ARowWhoseSaveFails_LeavesNoPhotosBehind_AndTheOtherRowsCarryOn()
    {
        _failingSave.ExternalRef = "GR-3";

        var job = await RunAsync((await UploadAsync(Csv(Row(PropertyType.Garage), Row(PropertyType.Apartment))))!.Id);

        Assert.Equal((2, 1, 1), (job.TotalRows, job.CreatedCount, job.FailedCount));
        var failed = await _db.ListingImportRows.SingleAsync(r => r.JobId == job.Id && r.ExternalRef == "GR-3");
        Assert.Equal(ErrorCodes.ImportRowFailed, Assert.Single(failed.Problems).Code);
        Assert.False(await _db.Listings.AnyAsync(l => l.ExternalRef == "GR-3"));

        // The garage's downloaded photo files (originals and display sizes) are deleted again; the
        // apartment's — saved — stay. (Blob names start with their listing's id.)
        var apartment = await ListingAsync("AP-1024");
        var garageBlobs = _blobs.UploadedContent.Keys.Where(name => !name.StartsWith(apartment.Id.ToString(), StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(garageBlobs);
        Assert.All(garageBlobs, name => Assert.Contains(name, _blobs.DeletedBlobNames));
        Assert.Equal(3, await _db.Photos.CountAsync(p => p.ListingId == apartment.Id));
        Assert.DoesNotContain(_blobs.DeletedBlobNames, name => name.StartsWith(apartment.Id.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task AFileThatsGone_FailsTheImport()
    {
        var upload = (await UploadAsync(Csv(Row(PropertyType.Garage))))!;
        _blobs.PrivateContent.Clear();

        var job = await RunAsync(upload.Id);

        Assert.Equal((ListingImportStatus.Failed, ErrorCodes.ImportFileMissing), (job.Status, job.FailureCode));
    }

    [Fact]
    public async Task Housekeeping_DeletesFilesAfter30Days_AndFailsImportsStuckForHours()
    {
        var old = ListingImportJob.Create(_agency.Id, _owner, "vechi.csv", "imports/old.csv", 10, Start.AddDays(-31));
        old.Start(0, Start.AddDays(-31));
        old.Complete(0, 0, 0, 0, 0, Start.AddDays(-31));
        var otherAgency = ListingTestData.AddAgency(_db, _owner, "Alta");
        var stuck = ListingImportJob.Create(otherAgency.Id, _owner, "blocat.csv", "imports/stuck.csv", 10, Start.AddHours(-3));
        stuck.Start(5, Start.AddHours(-3));
        _db.ListingImportJobs.AddRange(old, stuck);
        await _db.SaveChangesAsync();

        Assert.Equal(0, await Job().RunAsync(CancellationToken.None));

        _db.ChangeTracker.Clear();
        var oldAfter = await _db.ListingImportJobs.SingleAsync(j => j.Id == old.Id);
        Assert.Null(oldAfter.FileBlobName);
        Assert.Equal(Start, oldAfter.FileDeletedAt);
        Assert.Contains("imports/old.csv", _blobs.DeletedPrivateNames);
        var stuckAfter = await _db.ListingImportJobs.SingleAsync(j => j.Id == stuck.Id);
        Assert.Equal((ListingImportStatus.Failed, ErrorCodes.ImportWorkerFailed), (stuckAfter.Status, stuckAfter.FailureCode));
        Assert.Equal("imports/stuck.csv", stuckAfter.FileBlobName);
    }

    // Fails the save that would add the listing with ExternalRef (like a lost database connection),
    // once.
    private sealed class FailingListingSave : SaveChangesInterceptor
    {
        public string? ExternalRef { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (ExternalRef is { } externalRef
                && eventData.Context!.ChangeTracker.Entries<Listing>().Any(e => e.State == EntityState.Added && e.Entity.ExternalRef == externalRef))
            {
                ExternalRef = null;
                throw new DbUpdateException("Simulated: the connection was lost.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    [Fact]
    public async Task TheHistory_TheJob_ItsProblemRows_AndTheReport_AreReadable()
    {
        var job = await RunAsync((await UploadAsync(Csv(Row(PropertyType.Garage), Row(PropertyType.Room, ("bathroom_type", "")))))!.Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new GetListingImportsHandler(_db).Handle(new GetListingImportsQuery(_agency.Id, false), CancellationToken.None));
        var history = (await new GetListingImportsHandler(_db).Handle(new GetListingImportsQuery(_agency.Id, true), CancellationToken.None))!;
        var dto = Assert.Single(history);
        Assert.Equal((job.Id, "Completed", 2, 2, 1), (dto.Id, dto.Status, dto.TotalRows, dto.ProcessedRows, dto.FailedCount));

        var problems = (await new GetListingImportRowsHandler(_db).Handle(
            new GetListingImportRowsQuery(_agency.Id, job.Id, true, OnlyProblems: true), CancellationToken.None))!;
        var row = Assert.Single(problems.Items);
        Assert.Equal((3, "CM-12", "Failed"), (row.RowNumber, row.ExternalRef, row.Outcome));
        Assert.Contains(row.Problems, p => p.Column == "bathroom_type" && p.Code == ErrorCodes.ImportRequired);

        var report = Encoding.UTF8.GetString(ListingImportDtos.ReportCsv(problems.Items)[3..]);
        Assert.StartsWith("row;external_ref;outcome;severity;column;code;message", report);
        Assert.Contains($"3;CM-12;Failed;Error;bathroom_type;{ErrorCodes.ImportRequired};", report);

        Assert.Null(await new GetListingImportHandler(_db).Handle(new GetListingImportQuery(Guid.NewGuid(), job.Id, true), CancellationToken.None));
    }
}
