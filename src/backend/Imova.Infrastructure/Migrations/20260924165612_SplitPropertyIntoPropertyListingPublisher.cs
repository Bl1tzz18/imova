using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imova.Infrastructure.Migrations
{
    // Splits the old monolithic Properties table into Property (physical asset) + Listing (the
    // offer) + Publisher (who publishes it). Hand-written rather than scaffolded: the scaffolded
    // version dropped PropertyMedias and "renamed" unrelated columns into each other.
    //
    // Data mapping for existing rows:
    //  - every AspNetUsers row gets an Individual Publisher;
    //  - every Properties row becomes a Property (same Id) plus one Listing *with that same Id* —
    //    so photos (PropertyMedias.PropertyId), favorites, and /property/{id} URLs keep pointing
    //    at the right thing without any id remapping;
    //  - old Rooms/Floor/TotalFloors/Bathrooms move into the per-type TypeSpecificAttributes JSON,
    //    ParkingAvailable/Furnished become amenities, PetsAllowed/Furnished also seed a rental
    //    listing's RentalDetails.
    //
    // Down() is intentionally unsupported: reversing this means re-merging rows that can by then
    // have diverged (multiple listings per property, agency publishers). Restore from a backup.
    public partial class SplitPropertyIntoPropertyListingPublisher : Migration
    {
        // Must match AmenityConfiguration.ParkingId / FurnishedId.
        private const string ParkingAmenityId = "a1000000-0000-0000-0000-000000000001";
        private const string FurnishedAmenityId = "a1000000-0000-0000-0000-000000000005";

        // Must match ExchangeRateOptions' defaults — PriceEur for migrated MDL/USD listings.
        private const string MdlToEur = "0.051";
        private const string UsdToEur = "0.86";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Location data can't be fabricated, so a property without one is a hard stop rather
            // than something to paper over (every property has always been created with one).
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Properties" p
                        WHERE NOT EXISTS (SELECT 1 FROM "PropertyLocations" l WHERE l."PropertyId" = p."Id")
                    ) THEN
                        RAISE EXCEPTION 'SplitPropertyIntoPropertyListingPublisher: some Properties rows have no PropertyLocations row; fix them before migrating.';
                    END IF;
                END $$;
                """);

            // --- 1. Publishers: one Individual publisher per existing user ------------------------
            migrationBuilder.CreateTable(
                name: "Publishers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherType = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publishers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Publishers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Publishers_UserId_PublisherType",
                table: "Publishers",
                columns: new[] { "UserId", "PublisherType" },
                unique: true);

            // DisplayName falls back to the email's local part (same rule as
            // PublisherProvisioning.DisplayNameFor) — DisplayName is optional at registration.
            migrationBuilder.Sql("""
                INSERT INTO "Publishers" ("Id", "UserId", "PublisherType", "DisplayName", "Phone", "Email", "LogoUrl", "Bio", "CreatedAt")
                SELECT
                    gen_random_uuid(),
                    u."Id",
                    1,
                    left(COALESCE(NULLIF(btrim(u."DisplayName"), ''), split_part(COALESCE(u."Email", u."UserName", ''), '@', 1)), 200),
                    left(NULLIF(btrim(u."PhoneNumber"), ''), 20),
                    left(COALESCE(u."Email", u."UserName", ''), 256),
                    NULL,
                    NULL,
                    now()
                FROM "AspNetUsers" u;
                """);

            // --- 2. Amenities reference table -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "Amenities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LabelRo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_Key",
                table: "Amenities",
                column: "Key",
                unique: true);

            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "Key", "LabelRo" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), "parking", "Parcare" },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), "balcony", "Balcon/Logie" },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), "elevator", "Ascensor" },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), "air_conditioning", "Aer condiționat" },
                    { new Guid("a1000000-0000-0000-0000-000000000005"), "furnished", "Mobilat" },
                    { new Guid("a1000000-0000-0000-0000-000000000006"), "garage", "Garaj" },
                    { new Guid("a1000000-0000-0000-0000-000000000007"), "yard", "Curte" },
                    { new Guid("a1000000-0000-0000-0000-000000000008"), "autonomous_heating", "Încălzire autonomă" },
                    { new Guid("a1000000-0000-0000-0000-000000000009"), "centralized_heating", "Încălzire centralizată" },
                    { new Guid("a1000000-0000-0000-0000-00000000000a"), "wheelchair_access", "Acces pentru scaun cu rotile" },
                    { new Guid("a1000000-0000-0000-0000-00000000000b"), "storage_room", "Debara" },
                    { new Guid("a1000000-0000-0000-0000-00000000000c"), "video_surveillance", "Supraveghere video" },
                    { new Guid("a1000000-0000-0000-0000-00000000000d"), "internet", "Internet" }
                });

            // --- 3. Listings: one per existing property, reusing the property's Id ------------------
            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SaleDetails = table.Column<string>(type: "jsonb", nullable: true),
                    RentalDetails = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SuspensionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PriceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PriceIsNegotiable = table.Column<bool>(type: "boolean", nullable: false),
                    PriceEur = table.Column<decimal>(type: "numeric(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Listings_Publishers_PublisherId",
                        column: x => x.PublisherId,
                        principalTable: "Publishers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ListingType (Rent=1/Sale=2) and PropertyStatus 1–8 keep their integer values as
            // TransactionType/ListingStatus (Published -> Active = 3). JSON keys/enum names match
            // what ListingConfiguration's converters read.
            migrationBuilder.Sql($$"""
                INSERT INTO "Listings" (
                    "Id", "PropertyId", "PublisherId", "TransactionType", "Title", "Description",
                    "SaleDetails", "RentalDetails", "Status", "PublishedAt", "ExpiresAt", "CreatedAt", "UpdatedAt",
                    "RejectionReason", "SuspensionReason",
                    "PriceAmount", "PriceCurrency", "PriceIsNegotiable", "PriceEur")
                SELECT
                    p."Id",
                    p."Id",
                    pub."Id",
                    p."ListingType",
                    p."Title",
                    p."Description",
                    CASE WHEN p."ListingType" = 2 THEN '{}'::jsonb END,
                    CASE WHEN p."ListingType" = 1 THEN jsonb_build_object(
                        'minLeasePeriodMonths', NULL,
                        'securityDepositAmount', NULL,
                        'utilitiesIncluded', false,
                        'furnishedStatus', CASE WHEN p."Furnished" THEN 'Furnished' ELSE 'Unfurnished' END,
                        'availableFrom', NULL,
                        'petsAllowed', COALESCE(p."PetsAllowed", false)) END,
                    p."Status",
                    p."PublishedAt",
                    p."ExpiresAt",
                    p."CreatedAt",
                    p."UpdatedAt",
                    p."RejectionReason",
                    p."SuspensionReason",
                    p."Price",
                    upper(p."Currency"),
                    false,
                    round(p."Price" * CASE upper(p."Currency") WHEN 'MDL' THEN {{MdlToEur}} WHEN 'USD' THEN {{UsdToEur}} ELSE 1 END, 2)
                FROM "Properties" p
                JOIN "Publishers" pub ON pub."UserId" = p."OwnerId" AND pub."PublisherType" = 1;
                """);

            migrationBuilder.CreateIndex(name: "IX_Listings_PropertyId", table: "Listings", column: "PropertyId");
            migrationBuilder.CreateIndex(name: "IX_Listings_PublisherId", table: "Listings", column: "PublisherId");
            migrationBuilder.CreateIndex(name: "IX_Listings_Status", table: "Listings", column: "Status");

            // --- 4. PropertyAmenities from the old boolean columns -----------------------------------
            migrationBuilder.CreateTable(
                name: "PropertyAmenities",
                columns: table => new
                {
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyAmenities", x => new { x.PropertyId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_PropertyAmenities_Amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyAmenities_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAmenities_AmenityId",
                table: "PropertyAmenities",
                column: "AmenityId");

            migrationBuilder.Sql($"""
                INSERT INTO "PropertyAmenities" ("PropertyId", "AmenityId")
                SELECT "Id", '{ParkingAmenityId}'::uuid FROM "Properties" WHERE "ParkingAvailable" IS TRUE
                UNION ALL
                SELECT "Id", '{FurnishedAmenityId}'::uuid FROM "Properties" WHERE "Furnished" IS TRUE;
                """);

            // --- 5. Properties: physical-only columns -------------------------------------------------
            migrationBuilder.AlterColumn<int>(
                name: "YearBuilt",
                table: "Properties",
                type: "integer",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "Properties",
                type: "integer",
                nullable: true);

            // Area was optional for Room listings only; such rows get 0 and must be corrected on
            // their next edit (the validator requires TotalAreaM2 > 0).
            migrationBuilder.AddColumn<decimal>(
                name: "TotalAreaM2",
                table: "Properties",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.Sql("""UPDATE "Properties" SET "TotalAreaM2" = COALESCE("Area", 0);""");
            migrationBuilder.Sql("""ALTER TABLE "Properties" ALTER COLUMN "TotalAreaM2" DROP DEFAULT;""");

            // "kind" is the storage discriminator PropertyAttributesJson reads back. Only fields
            // that exist in each type's new schema carry over (see PropertyAttributes).
            migrationBuilder.AddColumn<string>(
                name: "TypeSpecificAttributes",
                table: "Properties",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
            migrationBuilder.Sql("""
                UPDATE "Properties" SET "TypeSpecificAttributes" = jsonb_strip_nulls(CASE "PropertyType"
                    WHEN 1 THEN jsonb_build_object(
                        'kind', 'Apartment',
                        'rooms', round("Rooms")::int,
                        'floor', "Floor",
                        'totalFloors', "TotalFloors",
                        'bathrooms', "Bathrooms")
                    WHEN 2 THEN jsonb_build_object(
                        'kind', 'House',
                        'rooms', round("Rooms")::int,
                        'houseFloors', "TotalFloors")
                    WHEN 3 THEN jsonb_build_object('kind', 'Land')
                    WHEN 4 THEN jsonb_build_object('kind', 'Commercial', 'floor', "Floor", 'mainStreetAccess', false)
                    WHEN 5 THEN jsonb_build_object('kind', 'Garage')
                    WHEN 6 THEN jsonb_build_object('kind', 'Room')
                END);
                """);
            migrationBuilder.Sql("""ALTER TABLE "Properties" ALTER COLUMN "TypeSpecificAttributes" DROP DEFAULT;""");

            // Property now points at its location, instead of the location pointing at the property.
            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "Properties",
                type: "uuid",
                nullable: true);
            migrationBuilder.Sql("""
                UPDATE "Properties" p SET "LocationId" = l."Id"
                FROM "PropertyLocations" l
                WHERE l."PropertyId" = p."Id";
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "Properties",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropForeignKey(name: "FK_PropertyLocations_Properties_PropertyId", table: "PropertyLocations");
            migrationBuilder.DropIndex(name: "IX_PropertyLocations_PropertyId", table: "PropertyLocations");
            migrationBuilder.DropColumn(name: "PropertyId", table: "PropertyLocations");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_LocationId",
                table: "Properties",
                column: "LocationId",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_Properties_PropertyType",
                table: "Properties",
                column: "PropertyType");
            migrationBuilder.AddForeignKey(
                name: "FK_Properties_PropertyLocations_LocationId",
                table: "Properties",
                column: "LocationId",
                principalTable: "PropertyLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Everything below was copied into Listings / PropertyAmenities / TypeSpecificAttributes above.
            migrationBuilder.DropForeignKey(name: "FK_Properties_AspNetUsers_OwnerId", table: "Properties");
            migrationBuilder.DropIndex(name: "IX_Properties_OwnerId", table: "Properties");
            migrationBuilder.DropIndex(name: "IX_Properties_Status", table: "Properties");
            foreach (var column in new[]
            {
                "OwnerId", "OrganizationId", "Title", "Description", "ListingType", "Status", "Price", "Currency",
                "Area", "Rooms", "Bathrooms", "Floor", "TotalFloors", "Furnished", "ParkingAvailable", "PetsAllowed",
                "PublishedAt", "ExpiresAt", "RejectionReason", "SuspensionReason",
            })
            {
                migrationBuilder.DropColumn(name: column, table: "Properties");
            }

            // --- 6. Favorites now reference Listings (same ids as the old properties) ---------------
            migrationBuilder.DropForeignKey(name: "FK_Favorites_Properties_PropertyId", table: "Favorites");
            migrationBuilder.RenameColumn(name: "PropertyId", table: "Favorites", newName: "ListingId");
            migrationBuilder.RenameIndex(name: "IX_Favorites_UserId_PropertyId", table: "Favorites", newName: "IX_Favorites_UserId_ListingId");
            migrationBuilder.RenameIndex(name: "IX_Favorites_PropertyId", table: "Favorites", newName: "IX_Favorites_ListingId");
            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Listings_ListingId",
                table: "Favorites",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // --- 7. PropertyMedias -> Photos, rows kept (PropertyId was already the listing's id) ----
            migrationBuilder.RenameTable(name: "PropertyMedias", newName: "Photos");
            migrationBuilder.Sql("""ALTER TABLE "Photos" RENAME CONSTRAINT "PK_PropertyMedias" TO "PK_Photos";""");
            migrationBuilder.RenameColumn(name: "PropertyId", table: "Photos", newName: "ListingId");
            migrationBuilder.RenameIndex(name: "IX_PropertyMedias_BlobName", table: "Photos", newName: "IX_Photos_BlobName");
            migrationBuilder.RenameIndex(name: "IX_PropertyMedias_PropertyId", table: "Photos", newName: "IX_Photos_ListingId");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "Photos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
            migrationBuilder.Sql("""ALTER TABLE "Photos" ALTER COLUMN "IsPrimary" DROP DEFAULT;""");

            // The first photo of each listing (same order the app displays them in) is its cover.
            migrationBuilder.Sql("""
                UPDATE "Photos" ph SET "IsPrimary" = true
                FROM (
                    SELECT DISTINCT ON ("ListingId") "Id"
                    FROM "Photos"
                    ORDER BY "ListingId", "SortOrder", "CreatedAt"
                ) first_photo
                WHERE ph."Id" = first_photo."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "SplitPropertyIntoPropertyListingPublisher is a one-way data migration (listings can no longer be " +
                "merged back into single property rows once properties have multiple listings or agency publishers). " +
                "Restore a database backup taken before it ran instead.");
        }
    }
}
