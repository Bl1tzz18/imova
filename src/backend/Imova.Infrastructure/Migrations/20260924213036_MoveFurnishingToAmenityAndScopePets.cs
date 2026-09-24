using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveFurnishingToAmenityAndScopePets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Furnishing is now only the property's "furnished" amenity (a single checkbox, for sale and
            // rent alike): a rental that was Furnished or PartiallyFurnished gets the amenity (the
            // "partially" nuance has no counterpart), then RentalDetails.furnishedStatus goes away.
            migrationBuilder.Sql("""
                INSERT INTO "PropertyAmenities" ("PropertyId", "AmenityId")
                SELECT DISTINCT l."PropertyId", 'a1000000-0000-0000-0000-000000000005'::uuid
                FROM "Listings" l
                JOIN "Properties" p ON p."Id" = l."PropertyId"
                WHERE l."RentalDetails"->>'furnishedStatus' IN ('Furnished', 'PartiallyFurnished')
                  AND p."PropertyType" IN (1, 2, 4, 6)
                ON CONFLICT DO NOTHING;

                UPDATE "Listings" SET "RentalDetails" = "RentalDetails" - 'furnishedStatus'
                WHERE "RentalDetails" ? 'furnishedStatus';
                """);

            // Pets are only asked for a rented apartment, house or room; drop the answer elsewhere.
            migrationBuilder.Sql("""
                UPDATE "Listings" l SET "RentalDetails" = l."RentalDetails" - 'petsAllowed'
                FROM "Properties" p
                WHERE p."Id" = l."PropertyId" AND p."PropertyType" NOT IN (1, 2, 6) AND l."RentalDetails" ? 'petsAllowed';
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only and not reversed: the furnishing level can't be recovered from the amenity.

        }
    }
}
