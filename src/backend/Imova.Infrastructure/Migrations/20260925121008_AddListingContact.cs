using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contact",
                table: "Listings",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contact",
                table: "Listings");
        }
    }
}
