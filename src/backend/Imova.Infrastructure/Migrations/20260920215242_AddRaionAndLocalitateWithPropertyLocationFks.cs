using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRaionAndLocalitateWithPropertyLocationFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "PropertyLocations");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "PropertyLocations",
                newName: "RaionName");

            migrationBuilder.AddColumn<Guid>(
                name: "LocalitateId",
                table: "PropertyLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalitateName",
                table: "PropertyLocations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RaionId",
                table: "PropertyLocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Raioane",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    NameRo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocalityLabel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raioane", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Localitati",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLocalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    NameRo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localitati", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Localitati_Localitati_ParentLocalityId",
                        column: x => x.ParentLocalityId,
                        principalTable: "Localitati",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Localitati_Raioane_RaionId",
                        column: x => x.RaionId,
                        principalTable: "Raioane",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyLocations_LocalitateId",
                table: "PropertyLocations",
                column: "LocalitateId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyLocations_RaionId",
                table: "PropertyLocations",
                column: "RaionId");

            migrationBuilder.CreateIndex(
                name: "IX_Localitati_ParentLocalityId",
                table: "Localitati",
                column: "ParentLocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Localitati_RaionId",
                table: "Localitati",
                column: "RaionId");

            migrationBuilder.CreateIndex(
                name: "IX_Localitati_SourceId",
                table: "Localitati",
                column: "SourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Raioane_SourceId",
                table: "Raioane",
                column: "SourceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyLocations_Localitati_LocalitateId",
                table: "PropertyLocations",
                column: "LocalitateId",
                principalTable: "Localitati",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyLocations_Raioane_RaionId",
                table: "PropertyLocations",
                column: "RaionId",
                principalTable: "Raioane",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyLocations_Localitati_LocalitateId",
                table: "PropertyLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyLocations_Raioane_RaionId",
                table: "PropertyLocations");

            migrationBuilder.DropTable(
                name: "Localitati");

            migrationBuilder.DropTable(
                name: "Raioane");

            migrationBuilder.DropIndex(
                name: "IX_PropertyLocations_LocalitateId",
                table: "PropertyLocations");

            migrationBuilder.DropIndex(
                name: "IX_PropertyLocations_RaionId",
                table: "PropertyLocations");

            migrationBuilder.DropColumn(
                name: "LocalitateId",
                table: "PropertyLocations");

            migrationBuilder.DropColumn(
                name: "LocalitateName",
                table: "PropertyLocations");

            migrationBuilder.DropColumn(
                name: "RaionId",
                table: "PropertyLocations");

            migrationBuilder.RenameColumn(
                name: "RaionName",
                table: "PropertyLocations",
                newName: "City");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "PropertyLocations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
