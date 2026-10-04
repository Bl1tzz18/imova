using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAlertSentAt",
                table: "Favorites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KnownPriceAmount",
                table: "Favorites",
                type: "numeric(14,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KnownPriceCurrency",
                table: "Favorites",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PriceAlertSentAt",
                table: "Favorites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailFavoriteUpdates",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Existing favorites start from today: the price they know is the current one, and a
            // listing that has already ended (Rented 6, Sold 7, Archived 8, Expired 9) sends nothing.
            migrationBuilder.Sql(
                """
                UPDATE "Favorites" f
                SET "KnownPriceAmount" = l."PriceAmount", "KnownPriceCurrency" = l."PriceCurrency"
                FROM "Listings" l
                WHERE l."Id" = f."ListingId";

                UPDATE "Favorites" f
                SET "EndedAlertSentAt" = now()
                FROM "Listings" l
                WHERE l."Id" = f."ListingId" AND l."Status" IN (6, 7, 8, 9);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndedAlertSentAt",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "KnownPriceAmount",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "KnownPriceCurrency",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "PriceAlertSentAt",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "EmailFavoriteUpdates",
                table: "AspNetUsers");
        }
    }
}
