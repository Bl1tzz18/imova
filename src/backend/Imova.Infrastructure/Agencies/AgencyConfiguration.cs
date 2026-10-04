using Imova.Application.Common.Identity;
using Imova.Domain.Agencies;
using Imova.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Agencies;

public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(Agency.MaxNameLength);
        builder.Property(a => a.Slug).IsRequired().HasMaxLength(AgencySlug.MaxLength);
        builder.HasIndex(a => a.Slug).IsUnique();
        builder.Property(a => a.LogoBlobName).HasMaxLength(500);
        builder.Property(a => a.Bio).HasMaxLength(Agency.MaxBioLength);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(Agency.MaxPhoneLength);
        builder.Property(a => a.Email).IsRequired().HasMaxLength(Agency.MaxEmailLength);
        builder.Property(a => a.Website).HasMaxLength(Agency.MaxWebsiteLength);
        builder.Property(a => a.Address).HasMaxLength(Agency.MaxAddressLength);
        builder.Property(a => a.Status).IsRequired();

        builder.HasOne<Raion>().WithMany().HasForeignKey(a => a.RaionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(a => a.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Members).WithOne().HasForeignKey(m => m.AgencyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Members).UsePropertyAccessMode(PropertyAccessMode.Field);

        // The directory: verified first, by city.
        builder.HasIndex(a => new { a.Status, a.IsVerified });
        builder.HasIndex(a => a.RaionId);
    }
}

public class AgencyMemberConfiguration : IEntityTypeConfiguration<AgencyMember>
{
    public void Configure(EntityTypeBuilder<AgencyMember> builder)
    {
        builder.ToTable("AgencyMembers");
        builder.HasKey(m => new { m.AgencyId, m.UserId });
        builder.Property(m => m.Role).IsRequired();

        // "My agencies". The account's deletion handles its memberships itself first (an agency's
        // last Owner can't just vanish — see AccountDeletion); the cascade is the backstop.
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => m.UserId);
    }
}

public class AgencyInvitationConfiguration : IEntityTypeConfiguration<AgencyInvitation>
{
    public void Configure(EntityTypeBuilder<AgencyInvitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Email).IsRequired().HasMaxLength(Agency.MaxEmailLength);
        builder.Property(i => i.Role).IsRequired();
        builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(i => i.TokenHash).IsUnique();

        builder.HasOne<Agency>().WithMany().HasForeignKey(i => i.AgencyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(i => i.InvitedByUserId).OnDelete(DeleteBehavior.SetNull);

        // One open invitation per address and agency — inviting again resends it.
        builder.HasIndex(i => new { i.AgencyId, i.Email })
            .IsUnique()
            .HasFilter("\"AcceptedAt\" IS NULL AND \"DeclinedAt\" IS NULL AND \"RevokedAt\" IS NULL");

        // "Invitations waiting for me" (by the account's email) and the per-agency hourly count.
        builder.HasIndex(i => i.Email);
        builder.HasIndex(i => new { i.AgencyId, i.LastSentAt });
    }
}

public class AgencyFormerSlugConfiguration : IEntityTypeConfiguration<AgencyFormerSlug>
{
    public void Configure(EntityTypeBuilder<AgencyFormerSlug> builder)
    {
        builder.ToTable("AgencySlugHistory");
        builder.HasKey(s => s.Slug);
        builder.Property(s => s.Slug).HasMaxLength(AgencySlug.MaxLength);
        builder.HasOne<Agency>().WithMany().HasForeignKey(s => s.AgencyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => s.AgencyId);
    }
}
