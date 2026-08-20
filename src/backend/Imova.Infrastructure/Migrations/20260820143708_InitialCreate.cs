using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PropertyType = table.Column<int>(type: "integer", nullable: false),
                    ListingType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Area = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    Rooms = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    Bathrooms = table.Column<short>(type: "smallint", nullable: true),
                    Floor = table.Column<short>(type: "smallint", nullable: true),
                    TotalFloors = table.Column<short>(type: "smallint", nullable: true),
                    YearBuilt = table.Column<short>(type: "smallint", nullable: true),
                    Furnished = table.Column<bool>(type: "boolean", nullable: true),
                    ParkingAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    PetsAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BuildingNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point,4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyLocations_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "LastLoginAt", "Phone" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo@imova.md", null, null });

            migrationBuilder.InsertData(
                table: "Properties",
                columns: new[] { "Id", "Area", "Bathrooms", "CreatedAt", "Currency", "Description", "ExpiresAt", "Floor", "Furnished", "ListingType", "OrganizationId", "OwnerId", "ParkingAvailable", "PetsAllowed", "Price", "PropertyType", "PublishedAt", "Rooms", "Status", "Title", "TotalFloors", "UpdatedAt", "YearBuilt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, null, new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", "Apartament luminos, renovat recent, aproape de centrul orasului.", null, null, null, 1, null, new Guid("33333333-3333-3333-3333-333333333333"), null, null, 550m, 1, new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, "Apartament cu 2 camere, Botanica", null, new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, null, new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", "Casa spatioasa cu curte proprie, ideala pentru o familie.", null, null, null, 2, null, new Guid("33333333-3333-3333-3333-333333333333"), null, null, 89000m, 2, new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, "Casa cu curte, Durlesti", null, new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                table: "PropertyLocations",
                columns: new[] { "Id", "BuildingNumber", "City", "Country", "District", "Latitude", "Location", "Longitude", "PropertyId", "Region", "Sector", "Street" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444441"), null, "Chisinau", "Moldova", "Botanica", 47.002099999999999, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (28.8681 47.0021)"), 28.868099999999998, new Guid("11111111-1111-1111-1111-111111111111"), null, null, null },
                    { new Guid("44444444-4444-4444-4444-444444444442"), null, "Chisinau", "Moldova", "Durlesti", 47.057499999999997, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (28.7397 47.0575)"), 28.739699999999999, new Guid("22222222-2222-2222-2222-222222222222"), null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_OwnerId",
                table: "Properties",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Status",
                table: "Properties",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyLocations_Location",
                table: "PropertyLocations",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyLocations_PropertyId",
                table: "PropertyLocations",
                column: "PropertyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyLocations");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
