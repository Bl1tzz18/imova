using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingNumberAndVisitorCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Number",
                table: "Listings",
                type: "bigint",
                nullable: false)
                .Annotation("Npgsql:IdentitySequenceOptions", "'100000', '1', '', '', 'False', '1'")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "PhoneRevealCount",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ListingVisitorMarks",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false),
                    VisitorHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CountedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingVisitorMarks", x => new { x.ListingId, x.Counter, x.VisitorHash });
                    table.ForeignKey(
                        name: "FK_ListingVisitorMarks_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Adding the identity column numbered the existing listings in whatever order Postgres keeps
            // them; renumber them oldest first, then carry the sequence on from the last one.
            migrationBuilder.Sql("""
                UPDATE "Listings" AS l SET "Number" = n.rn + 99999
                FROM (SELECT "Id", row_number() OVER (ORDER BY "CreatedAt", "Id") AS rn FROM "Listings") AS n
                WHERE l."Id" = n."Id";
                SELECT setval(pg_get_serial_sequence('"Listings"', 'Number'), (SELECT COALESCE(MAX("Number"), 99999) FROM "Listings"));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Number",
                table: "Listings",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListingVisitorMarks_CountedAt",
                table: "ListingVisitorMarks",
                column: "CountedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingVisitorMarks");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Number",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PhoneRevealCount",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Listings");
        }
    }
}
