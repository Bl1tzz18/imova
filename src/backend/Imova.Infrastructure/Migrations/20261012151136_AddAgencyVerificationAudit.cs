using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyVerificationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TargetUserId",
                table: "AdminAuditEntries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetAgencyId",
                table: "AdminAuditEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEntries_TargetAgencyId",
                table: "AdminAuditEntries",
                column: "TargetAgencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminAuditEntries_TargetAgencyId",
                table: "AdminAuditEntries");

            migrationBuilder.DropColumn(
                name: "TargetAgencyId",
                table: "AdminAuditEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "TargetUserId",
                table: "AdminAuditEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
