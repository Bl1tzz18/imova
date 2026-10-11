using System.Text.Json;
using System.Text.Json.Serialization;
using Imova.Application.Common.Identity;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Agencies;

public class ListingImportJobConfiguration : IEntityTypeConfiguration<ListingImportJob>
{
    public void Configure(EntityTypeBuilder<ListingImportJob> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.FileName).IsRequired().HasMaxLength(ListingImportJob.MaxFileNameLength);
        builder.Property(j => j.FileBlobName).HasMaxLength(500);
        builder.Property(j => j.Status).IsRequired();
        builder.Property(j => j.FailureCode).HasMaxLength(100);

        builder.HasOne<Agency>().WithMany().HasForeignKey(j => j.AgencyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(j => j.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);

        // An agency's history (newest first), and "is one already running?".
        builder.HasIndex(j => new { j.AgencyId, j.CreatedAt });

        // What the Worker looks for: the oldest waiting job; files past KeepFileFor.
        builder.HasIndex(j => new { j.Status, j.CreatedAt });
    }
}

public class ListingImportRowConfiguration : IEntityTypeConfiguration<ListingImportRow>
{
    private static readonly JsonSerializerOptions ProblemsJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Configure(EntityTypeBuilder<ListingImportRow> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ExternalRef).HasMaxLength(ListingImportRow.MaxExternalRefLength);
        builder.Property(r => r.Outcome).IsRequired();

        // Written once, never changed — the comparer only has to be correct, not clever.
        builder.Property(r => r.Problems)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(
                problems => JsonSerializer.Serialize(problems, ProblemsJsonOptions),
                json => JsonSerializer.Deserialize<List<ListingImportProblem>>(json, ProblemsJsonOptions) ?? new List<ListingImportProblem>(),
                new ValueComparer<IReadOnlyList<ListingImportProblem>>(
                    (a, b) => JsonSerializer.Serialize(a, ProblemsJsonOptions) == JsonSerializer.Serialize(b, ProblemsJsonOptions),
                    list => JsonSerializer.Serialize(list, ProblemsJsonOptions).GetHashCode(),
                    list => list.ToList()));

        builder.HasOne<ListingImportJob>().WithMany().HasForeignKey(r => r.JobId).OnDelete(DeleteBehavior.Cascade);

        // A job's rows in file order; the error report reads the failed and warned ones.
        builder.HasIndex(r => new { r.JobId, r.RowNumber });

        // No FK to Listing: a row keeps saying what happened even after its listing is deleted.
    }
}
