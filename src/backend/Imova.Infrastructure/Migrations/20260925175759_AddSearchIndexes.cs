using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_PriceEur",
                table: "Listings",
                column: "PriceEur");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Status_TransactionType",
                table: "Listings",
                columns: new[] { "Status", "TransactionType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_PriceEur",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Status_TransactionType",
                table: "Listings");
        }
    }
}
