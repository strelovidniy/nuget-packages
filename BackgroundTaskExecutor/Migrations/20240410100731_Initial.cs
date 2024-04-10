using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackgroundTaskExecutor.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    MachineName = table.Column<string>(maxLength: 200, nullable: false),
                    TaskName = table.Column<string>(maxLength: 200, nullable: false),
                    LastRun = table.Column<DateTime>(nullable: false),
                    Profile = table.Column<string>(maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEntries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncEntries");
        }
    }
}
