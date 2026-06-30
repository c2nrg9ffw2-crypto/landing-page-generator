using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCH.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadlineNotified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeadlineNotified",
                table: "tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadlineNotified",
                table: "tasks");
        }
    }
}
