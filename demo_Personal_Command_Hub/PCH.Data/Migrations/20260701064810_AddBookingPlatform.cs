using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCH.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "bookings",
                type: "TEXT",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Platform",
                table: "bookings");
        }
    }
}
