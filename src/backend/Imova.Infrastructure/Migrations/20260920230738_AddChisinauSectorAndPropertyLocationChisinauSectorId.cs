using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChisinauSectorAndPropertyLocationChisinauSectorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChisinauSectorId",
                table: "PropertyLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChisinauSectorName",
                table: "PropertyLocations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChisinauSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChisinauSectors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyLocations_ChisinauSectorId",
                table: "PropertyLocations",
                column: "ChisinauSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChisinauSectors_Name",
                table: "ChisinauSectors",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyLocations_ChisinauSectors_ChisinauSectorId",
                table: "PropertyLocations",
                column: "ChisinauSectorId",
                principalTable: "ChisinauSectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyLocations_ChisinauSectors_ChisinauSectorId",
                table: "PropertyLocations");

            migrationBuilder.DropTable(
                name: "ChisinauSectors");

            migrationBuilder.DropIndex(
                name: "IX_PropertyLocations_ChisinauSectorId",
                table: "PropertyLocations");

            migrationBuilder.DropColumn(
                name: "ChisinauSectorId",
                table: "PropertyLocations");

            migrationBuilder.DropColumn(
                name: "ChisinauSectorName",
                table: "PropertyLocations");
        }
    }
}
