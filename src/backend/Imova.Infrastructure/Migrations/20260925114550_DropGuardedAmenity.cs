using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropGuardedAmenity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Properties that had "guarded" just lose it (no replacement). PropertyAmenities' FK to
            // Amenities is Restrict — the join rows have to go before the amenity row can.
            migrationBuilder.Sql("""
                DELETE FROM "PropertyAmenities"
                WHERE "AmenityId" = 'a1000000-0000-0000-0000-000000000027';
                """);

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000027"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "ApplicablePropertyTypes", "Category", "Key", "LabelRo" },
                values: new object[] { new Guid("a1000000-0000-0000-0000-000000000027"), new[] { 3, 5 }, "Security", "guarded", "Pază" });
        }
    }
}
