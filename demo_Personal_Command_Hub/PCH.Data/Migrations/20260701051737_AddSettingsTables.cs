using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCH.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImapHost = table.Column<string>(type: "TEXT", nullable: false),
                    ImapPort = table.Column<int>(type: "INTEGER", nullable: false),
                    ImapSsl = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImapUsername = table.Column<string>(type: "TEXT", nullable: true),
                    ImapPassword = table.Column<string>(type: "TEXT", nullable: true),
                    NotifyNewTask = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyDeadlineToday = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyDailyNewsSummary = table.Column<bool>(type: "INTEGER", nullable: false),
                    NewsSummaryHour = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rss_feeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rss_feeds", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "rss_feeds");
        }
    }
}
