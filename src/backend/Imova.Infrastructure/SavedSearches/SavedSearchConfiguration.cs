using Imova.Application.Common.Identity;
using Imova.Domain.SavedSearches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.SavedSearches;

public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => s.UserId);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(SavedSearch.MaxNameLength);
        builder.Property(s => s.QueryString).IsRequired().HasMaxLength(SavedSearch.MaxQueryLength);
        builder.Property(s => s.AlertFrequency).IsRequired();

        // The alert job's scan: every search with alerts on.
        builder.HasIndex(s => s.AlertFrequency);
    }
}
