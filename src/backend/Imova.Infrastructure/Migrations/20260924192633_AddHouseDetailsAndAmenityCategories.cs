using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseDetailsAndAmenityCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Amenities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "Category",
                value: "Leisure");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "Category",
                value: "Comfort");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "Category",
                value: "General");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "Category",
                value: "Comfort");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "Category",
                value: "Comfort");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "Category",
                value: "Leisure");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "Category",
                value: "Leisure");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "Category",
                value: "General");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "Category",
                value: "General");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000a"),
                column: "Category",
                value: "Security");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000b"),
                column: "Category",
                value: "Leisure");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000c"),
                column: "Category",
                value: "Security");

            migrationBuilder.UpdateData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000d"),
                column: "Category",
                value: "Comfort");

            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "Category", "Key", "LabelRo" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-00000000000e"), "Comfort", "fireplace", "Șemineu" },
                    { new Guid("a1000000-0000-0000-0000-00000000000f"), "Comfort", "underfloor_heating", "Încălzire în pardoseală" },
                    { new Guid("a1000000-0000-0000-0000-000000000010"), "Comfort", "smart_home", "Sistem casă inteligentă" },
                    { new Guid("a1000000-0000-0000-0000-000000000011"), "Comfort", "appliances", "Cu tehnică de uz casnic" },
                    { new Guid("a1000000-0000-0000-0000-000000000012"), "Comfort", "cable_tv", "Televiziune prin cablu" },
                    { new Guid("a1000000-0000-0000-0000-000000000013"), "Comfort", "landline", "Telefon fix" },
                    { new Guid("a1000000-0000-0000-0000-000000000014"), "Security", "intercom", "Interfon" },
                    { new Guid("a1000000-0000-0000-0000-000000000015"), "Security", "alarm_system", "Sistem de alarmă" },
                    { new Guid("a1000000-0000-0000-0000-000000000016"), "Security", "remote_gate", "Poartă cu telecomandă" },
                    { new Guid("a1000000-0000-0000-0000-000000000017"), "Leisure", "sauna", "Saună" },
                    { new Guid("a1000000-0000-0000-0000-000000000018"), "Leisure", "basement", "Beci/subsol" },
                    { new Guid("a1000000-0000-0000-0000-000000000019"), "Leisure", "gazebo", "Foișor" },
                    { new Guid("a1000000-0000-0000-0000-00000000001a"), "Leisure", "pool", "Piscină" },
                    { new Guid("a1000000-0000-0000-0000-00000000001b"), "Leisure", "terrace", "Terasă" },
                    { new Guid("a1000000-0000-0000-0000-00000000001c"), "Leisure", "garden", "Grădină/seră" },
                    { new Guid("a1000000-0000-0000-0000-00000000001d"), "Leisure", "staff_room", "Cameră pentru personal/pază" },
                    { new Guid("a1000000-0000-0000-0000-00000000001e"), "Leisure", "backup_generator", "Generator de rezervă" },
                    { new Guid("a1000000-0000-0000-0000-00000000001f"), "Leisure", "water_purification", "Sistem de purificare a apei" }
                });

            // Existing House rows: carry over what maps onto the new schema — constructionType
            // becomes buildingMaterial (Stone has no counterpart, so it becomes Other),
            // utilities.gas becomes gasSupply, and the generic Property.Condition moves into the
            // more granular houseCondition where there's a clear equivalent (Renovated ->
            // EuroRenovated, RedStructure -> Unfinished; New has none). The old keys are removed.
            // Water/sewage booleans can't be turned into the new WaterSupply/Sewerage choices
            // (which source? which system?) — the owner fills those in on their next edit,
            // which the validator now requires anyway.
            migrationBuilder.Sql("""
                UPDATE "Properties" SET
                    "TypeSpecificAttributes" = jsonb_strip_nulls(
                        ("TypeSpecificAttributes" - 'constructionType' - 'utilities')
                        || jsonb_build_object(
                            'buildingMaterial', CASE "TypeSpecificAttributes"->>'constructionType'
                                WHEN 'Brick' THEN 'Brick'
                                WHEN 'Wood' THEN 'Wood'
                                WHEN 'Stone' THEN 'Other'
                                WHEN 'Other' THEN 'Other'
                            END,
                            'gasSupply', COALESCE(("TypeSpecificAttributes"->'utilities'->>'gas')::boolean, false),
                            'houseCondition', CASE "Condition"
                                WHEN 2 THEN 'EuroRenovated'
                                WHEN 3 THEN 'NeedsRepair'
                                WHEN 4 THEN 'GrayStructure'
                                WHEN 5 THEN 'Unfinished'
                            END)),
                    "Condition" = NULL
                WHERE "PropertyType" = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Schema only — the House JSON conversion in Up() is not reversed (the old keys are gone).
            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000e"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000f"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001a"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001b"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001c"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001d"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001e"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000001f"));

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Amenities");
        }
    }
}
