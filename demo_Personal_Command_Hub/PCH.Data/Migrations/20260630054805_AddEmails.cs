using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCH.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Sender = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    BodyPreview = table.Column<string>(type: "TEXT", nullable: false),
                    IsKeywordMatch = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailType = table.Column<int>(type: "INTEGER", nullable: false),
                    LlmSummary = table.Column<string>(type: "TEXT", nullable: true),
                    LinkedTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    FetchedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_emails_MessageId",
                table: "emails",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emails");
        }
    }
}
