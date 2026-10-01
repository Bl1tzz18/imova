using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingPriceChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingPriceChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    OldCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OldPriceEur = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    NewAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    NewCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    NewPriceEur = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingPriceChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingPriceChanges_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingPriceChanges_ListingId_ChangedAt",
                table: "ListingPriceChanges",
                columns: new[] { "ListingId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingPriceChanges");
        }
    }
}
