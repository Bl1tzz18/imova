using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingDailyStatsAndAgencyPhoneReveals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgencyPhoneReveals",
                columns: table => new
                {
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    VisitorHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RevealedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyPhoneReveals", x => new { x.AgencyId, x.Day, x.VisitorHash });
                    table.ForeignKey(
                        name: "FK_AgencyPhoneReveals_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListingDailyStats",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    PhoneReveals = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingDailyStats", x => new { x.ListingId, x.Day });
                    table.ForeignKey(
                        name: "FK_ListingDailyStats_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyPhoneReveals_RevealedAt",
                table: "AgencyPhoneReveals",
                column: "RevealedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ListingDailyStats_Day",
                table: "ListingDailyStats",
                column: "Day");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgencyPhoneReveals");

            migrationBuilder.DropTable(
                name: "ListingDailyStats");
        }
    }
}
