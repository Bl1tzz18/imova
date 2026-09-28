using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiryReminderSentAt",
                table: "Listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Status_ExpiresAt",
                table: "Listings",
                columns: new[] { "Status", "ExpiresAt" });

            // Listings that are already live (or suspended) never got an expiry date. Give them the
            // usual 6 months from publication, but at least 14 days from now so the owner still
            // gets the reminder email before anything expires. Status 3 = Active, 5 = Suspended.
            migrationBuilder.Sql("""
                UPDATE "Listings"
                SET "ExpiresAt" = GREATEST(
                    COALESCE("PublishedAt", now()) + interval '6 months',
                    now() + interval '14 days')
                WHERE "Status" IN (3, 5) AND "ExpiresAt" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_Status_ExpiresAt",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ExpiryReminderSentAt",
                table: "Listings");
        }
    }
}
