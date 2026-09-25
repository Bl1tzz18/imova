using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizePropertyTypeDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "ApplicablePropertyTypes",
                table: "Amenities",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                columns: new[] { "ApplicablePropertyTypes", "Category" },
                values: new object[] { new[] { 1, 2, 6 }, "General" });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 4 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4, 6 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4, 6 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "ApplicablePropertyTypes",
                value: new[] { 4, 6 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "ApplicablePropertyTypes",
                value: new[] { 4, 6 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000a"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000b"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000c"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4, 5 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000d"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4, 6 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000e"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000f"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 6 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 6 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "ApplicablePropertyTypes",
                value: new[] { 1, 2, 4, 5 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000016"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2, 5 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000017"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000018"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000019"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001a"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001b"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001c"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001d"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001e"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2, 4 });

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001f"),
                column: "ApplicablePropertyTypes",
                value: new[] { 2 });

            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "ApplicablePropertyTypes", "Category", "Key", "LabelRo" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000020"), new[] { 1, 2, 6 }, "Comfort", "dishwasher", "Mașină de spălat vase" },
                    { new Guid("a1000000-0000-0000-0000-000000000021"), new[] { 1, 2 }, "Security", "armored_door", "Ușă blindată" },
                    { new Guid("a1000000-0000-0000-0000-000000000022"), new[] { 1 }, "General", "storage_annex", "Anexă/boxă" },
                    { new Guid("a1000000-0000-0000-0000-000000000023"), new[] { 1, 4 }, "General", "separate_entrance", "Intrare separată" },
                    { new Guid("a1000000-0000-0000-0000-000000000024"), new[] { 1, 2, 4 }, "Comfort", "panoramic_windows", "Geamuri panoramice" },
                    { new Guid("a1000000-0000-0000-0000-000000000025"), new[] { 1 }, "Comfort", "separate_living_room", "Living separat" },
                    { new Guid("a1000000-0000-0000-0000-000000000026"), new[] { 1, 4, 6 }, "Comfort", "thermopane_windows", "Geamuri termopan" },
                    { new Guid("a1000000-0000-0000-0000-000000000027"), new[] { 3, 5 }, "Security", "guarded", "Pază" },
                    { new Guid("a1000000-0000-0000-0000-000000000028"), new[] { 3, 2 }, "Leisure", "near_water", "Lângă un bazin acvatic" },
                    { new Guid("a1000000-0000-0000-0000-000000000029"), new[] { 3, 2 }, "Leisure", "near_forest", "Lângă pădure" },
                    { new Guid("a1000000-0000-0000-0000-00000000002a"), new[] { 5 }, "General", "electricity", "Electricitate" },
                    { new Guid("a1000000-0000-0000-0000-00000000002b"), new[] { 6 }, "Comfort", "kitchen_access", "Acces la bucătărie" }
                });

            // Existing rows onto the generalized per-type schemas. General Property.Condition values
            // (1 New, 2 Renovated, 3 NeedsRepair, 4 GrayStructure, 5 RedStructure) move into
            // finishCondition for House/Apartment/Commercial where there's an equivalent, and
            // Condition is cleared for those types (Garage/Room keep it). Anything without a
            // counterpart is left for the owner to fill in — the validators require it on the next
            // edit.
            migrationBuilder.Sql("""
                UPDATE "Properties" p SET
                    "TypeSpecificAttributes" = jsonb_strip_nulls(CASE p."PropertyType"
                        -- Apartment: heatingType -> heatingSystem; condition -> finishCondition;
                        -- a "New" apartment is new construction.
                        WHEN 1 THEN (a - 'heatingType') || jsonb_build_object(
                            'heatingSystem', CASE a->>'heatingType'
                                WHEN 'Centralized' THEN 'DistrictHeating'
                                WHEN 'Autonomous' THEN 'OwnBoiler'
                            END,
                            'finishCondition', COALESCE(a->>'finishCondition', finish),
                            'housingStockType', COALESCE(a->>'housingStockType', CASE WHEN p."Condition" = 1 THEN 'NewConstruction' END))
                        -- House: houseCondition is now the shared finishCondition key.
                        WHEN 2 THEN (a - 'houseCondition') || jsonb_build_object(
                            'finishCondition', COALESCE(a->>'finishCondition', a->>'houseCondition'))
                        -- Land: landDesignation splits into plotType / locationContext; boundary
                        -- utilities become individual yes/no fields (water has no counterpart).
                        WHEN 3 THEN (a - 'landDesignation' - 'utilitiesAtBoundary') || jsonb_build_object(
                            'plotType', CASE a->>'landDesignation'
                                WHEN 'Agricultural' THEN 'Agricultural'
                                WHEN 'Construction' THEN 'ForConstruction'
                            END,
                            'locationContext', CASE a->>'landDesignation'
                                WHEN 'Intravilan' THEN 'WithinTownLimits'
                                WHEN 'Extravilan' THEN 'OutsideTownLimits'
                            END,
                            'gasPipelineAtBoundary', (a->'utilitiesAtBoundary'->>'gas')::boolean,
                            'electricitySupplyAtBoundary', (a->'utilitiesAtBoundary'->>'electricity')::boolean)
                        -- Commercial: the four old space types map onto the new list.
                        WHEN 4 THEN a || jsonb_build_object(
                            'spaceType', CASE a->>'spaceType'
                                WHEN 'Office' THEN 'OfficeSpace'
                                WHEN 'Retail' THEN 'RetailSpace'
                                WHEN 'HoReCa' THEN 'FoodServiceSpace'
                                ELSE a->>'spaceType'
                            END,
                            'finishCondition', COALESCE(a->>'finishCondition', finish))
                        -- Garage: garageType -> parkingType.
                        WHEN 5 THEN (a - 'garageType') || jsonb_build_object(
                            'parkingType', CASE a->>'garageType'
                                WHEN 'Underground' THEN 'UndergroundParking'
                                WHEN 'Box' THEN 'Garage'
                                WHEN 'Individual' THEN 'Garage'
                            END)
                        -- Room: privateOrSharedBathroom -> bathroomType.
                        WHEN 6 THEN (a - 'privateOrSharedBathroom') || jsonb_build_object(
                            'bathroomType', a->>'privateOrSharedBathroom')
                    END),
                    "Condition" = CASE WHEN p."PropertyType" IN (1, 2, 4) THEN NULL ELSE p."Condition" END
                FROM (
                    SELECT "Id" AS id,
                           "TypeSpecificAttributes" AS a,
                           CASE "Condition"
                               WHEN 2 THEN 'EuroRenovated'
                               WHEN 3 THEN 'NeedsRepair'
                               WHEN 4 THEN 'GrayStructure'
                               WHEN 5 THEN 'Unfinished'
                           END AS finish
                    FROM "Properties"
                ) src
                WHERE p."Id" = src.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Schema/seed only — the attribute JSON conversion in Up() is not reversed.
            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000002a"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000002b"));

            migrationBuilder.DropColumn(
                name: "ApplicablePropertyTypes",
                table: "Amenities");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "Category",
                value: "Comfort");
        }
    }
}
