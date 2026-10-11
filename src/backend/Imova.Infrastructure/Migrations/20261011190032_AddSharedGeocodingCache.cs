using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedGeocodingCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeocodedAddresses",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    FormattedAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LookedUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeocodedAddresses", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "GeocoderThrottle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    NextSlotAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeocoderThrottle", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "GeocoderThrottle",
                columns: new[] { "Id", "NextSlotAt" },
                values: new object[] { 1, new DateTimeOffset(new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeocodedAddresses");

            migrationBuilder.DropTable(
                name: "GeocoderThrottle");
        }
    }
}
