using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgencyId",
                table: "Listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalRef",
                table: "Listings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    LogoBlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Website = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    RaionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agencies_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Agencies_Raioane_RaionId",
                        column: x => x.RaionId,
                        principalTable: "Raioane",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgencyMembers",
                columns: table => new
                {
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyMembers", x => new { x.AgencyId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AgencyMembers_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgencyMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgencySlugHistory",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencySlugHistory", x => x.Slug);
                    table.ForeignKey(
                        name: "FK_AgencySlugHistory_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            MoveAgencyPublishersToAgencies(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_Publishers_UserId_PublisherType",
                table: "Publishers");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Publishers");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Publishers");

            migrationBuilder.DropColumn(
                name: "PublisherType",
                table: "Publishers");

            migrationBuilder.CreateIndex(
                name: "IX_Publishers_UserId",
                table: "Publishers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_AgencyId",
                table: "Listings",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_AgencyId_ExternalRef",
                table: "Listings",
                columns: new[] { "AgencyId", "ExternalRef" },
                unique: true,
                filter: "\"ExternalRef\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_CreatedByUserId",
                table: "Agencies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_RaionId",
                table: "Agencies",
                column: "RaionId");

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_Slug",
                table: "Agencies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_Status_IsVerified",
                table: "Agencies",
                columns: new[] { "Status", "IsVerified" });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyMembers_UserId",
                table: "AgencyMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgencySlugHistory_AgencyId",
                table: "AgencySlugHistory",
                column: "AgencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Agencies_AgencyId",
                table: "Listings",
                column: "AgencyId",
                principalTable: "Agencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "AddAgencies is a one-way data migration (agency publishers became agencies and their listings moved to " +
                "their owners' own publishers). Restore from a backup instead.");
        }

        // Every agency publisher becomes an agency with the same Id, its user that agency's Owner; the
        // agency's listings keep their row but move to the owner's own publisher with AgencyId set; then
        // the agency publishers are deleted. A user who somehow had no individual publisher gets one first.
        //
        // Slugs are made in SQL with the same rules as AgencySlug for Latin names (lower case, Romanian
        // and common accented letters spelled plainly, anything else a dash, numbered on a clash, the
        // reserved words numbered); a name with no Latin letters or digits (e.g. Cyrillic) gets
        // "agentie-N" and can be renamed. Logos aren't carried over: they were outside URLs, and logos
        // are now uploaded only.
        private static void MoveAgencyPublishersToAgencies(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Publishers" ("Id", "UserId", "PublisherType", "DisplayName", "Phone", "Email", "CreatedAt")
                SELECT gen_random_uuid(), a."UserId", 1,
                       left(COALESCE(NULLIF(trim(u."DisplayName"), ''), split_part(COALESCE(u."Email", u."UserName", ''), '@', 1)), 200),
                       left(u."PhoneNumber", 20),
                       COALESCE(u."Email", u."UserName", ''),
                       now()
                FROM "Publishers" a
                JOIN "AspNetUsers" u ON u."Id" = a."UserId"
                WHERE a."PublisherType" = 2
                  AND NOT EXISTS (SELECT 1 FROM "Publishers" i WHERE i."UserId" = a."UserId" AND i."PublisherType" = 1);
                """);

            migrationBuilder.Sql("""
                WITH src AS (
                    SELECT p."Id", p."UserId", p."DisplayName", p."Phone", p."Email", p."Bio", p."CreatedAt",
                           COALESCE(NULLIF(trim(BOTH '-' FROM left(trim(BOTH '-' FROM regexp_replace(
                               translate(lower(p."DisplayName"), 'ăâîșşțţáàäéèêëíóöôúüçñ', 'aaissttaaaeeeeiooouucn'),
                               '[^a-z0-9]+', '-', 'g')), 130)), ''), 'agentie') AS base
                    FROM "Publishers" p
                    WHERE p."PublisherType" = 2
                ),
                ranked AS (
                    SELECT src.*,
                           row_number() OVER (PARTITION BY base ORDER BY "CreatedAt", "Id") AS rn,
                           base IN ('new', 'edit', 'admin', 'api') AS reserved
                    FROM src
                )
                INSERT INTO "Agencies" ("Id", "Name", "Slug", "Bio", "Phone", "Email", "IsVerified", "Status",
                                        "CreatedAt", "UpdatedAt", "CreatedByUserId")
                SELECT "Id",
                       left(trim("DisplayName"), 120),
                       CASE WHEN rn = 1 AND NOT reserved THEN base
                            ELSE base || '-' || (rn + CASE WHEN reserved THEN 1 ELSE 0 END) END,
                       "Bio",
                       COALESCE("Phone", ''),
                       left("Email", 254),
                       false,
                       1,
                       "CreatedAt",
                       "CreatedAt",
                       "UserId"
                FROM ranked;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "AgencyMembers" ("AgencyId", "UserId", "Role", "JoinedAt")
                SELECT "Id", "UserId", 1, "CreatedAt" FROM "Publishers" WHERE "PublisherType" = 2;
                """);

            migrationBuilder.Sql("""
                UPDATE "Listings" l
                SET "AgencyId" = a."Id", "PublisherId" = i."Id"
                FROM "Publishers" a
                JOIN "Publishers" i ON i."UserId" = a."UserId" AND i."PublisherType" = 1
                WHERE l."PublisherId" = a."Id" AND a."PublisherType" = 2;
                """);

            migrationBuilder.Sql("""DELETE FROM "Publishers" WHERE "PublisherType" = 2;""");
        }
    }
}
