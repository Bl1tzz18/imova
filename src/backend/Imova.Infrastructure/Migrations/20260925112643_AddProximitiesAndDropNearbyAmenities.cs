using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProximitiesAndDropNearbyAmenities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "near_water"/"near_forest" are dropped with no counterpart among the new proximities,
            // so properties that had them just lose them. PropertyAmenities' FK to Amenities is
            // Restrict — the join rows have to go before the amenity rows can.
            migrationBuilder.Sql("""
                DELETE FROM "PropertyAmenities"
                WHERE "AmenityId" IN ('a1000000-0000-0000-0000-000000000028', 'a1000000-0000-0000-0000-000000000029');
                """);

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000029"));

            migrationBuilder.CreateTable(
                name: "Proximities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LabelRo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApplicablePropertyTypes = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proximities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyProximities",
                columns: table => new
                {
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProximityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyProximities", x => new { x.PropertyId, x.ProximityId });
                    table.ForeignKey(
                        name: "FK_PropertyProximities_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropertyProximities_Proximities_ProximityId",
                        column: x => x.ProximityId,
                        principalTable: "Proximities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Proximities",
                columns: new[] { "Id", "ApplicablePropertyTypes", "Key", "LabelRo" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-0000-0000-000000000001"), new[] { 1, 2, 3, 4, 5, 6 }, "kindergarten", "Grădiniță" },
                    { new Guid("b1000000-0000-0000-0000-000000000002"), new[] { 1, 2, 3, 4, 5, 6 }, "school", "Școală" },
                    { new Guid("b1000000-0000-0000-0000-000000000003"), new[] { 1, 2, 3, 4, 5, 6 }, "supermarket", "Supermarket / magazin alimentar" },
                    { new Guid("b1000000-0000-0000-0000-000000000004"), new[] { 1, 2, 3, 4, 5, 6 }, "pharmacy", "Farmacie" },
                    { new Guid("b1000000-0000-0000-0000-000000000005"), new[] { 1, 2, 3, 4, 5, 6 }, "public_transport", "Stație transport public" },
                    { new Guid("b1000000-0000-0000-0000-000000000006"), new[] { 1, 2, 3, 4, 5, 6 }, "park", "Parc / zonă verde" },
                    { new Guid("b1000000-0000-0000-0000-000000000007"), new[] { 1, 2, 3, 4, 5, 6 }, "city_center", "Centrul orașului" },
                    { new Guid("b1000000-0000-0000-0000-000000000008"), new[] { 1, 2, 3, 4, 5, 6 }, "hospital", "Spital / policlinică" },
                    { new Guid("b1000000-0000-0000-0000-000000000009"), new[] { 1, 2, 3, 4, 5, 6 }, "farmers_market", "Piață agroalimentară" },
                    { new Guid("b1000000-0000-0000-0000-00000000000a"), new[] { 1, 2, 3, 4, 5, 6 }, "bank", "Bancă / bancomat" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyProximities_ProximityId",
                table: "PropertyProximities",
                column: "ProximityId");

            migrationBuilder.CreateIndex(
                name: "IX_Proximities_Key",
                table: "Proximities",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyProximities");

            migrationBuilder.DropTable(
                name: "Proximities");

            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "ApplicablePropertyTypes", "Category", "Key", "LabelRo" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000028"), new[] { 3, 2 }, "Leisure", "near_water", "Lângă un bazin acvatic" },
                    { new Guid("a1000000-0000-0000-0000-000000000029"), new[] { 3, 2 }, "Leisure", "near_forest", "Lângă pădure" }
                });
        }
    }
}
