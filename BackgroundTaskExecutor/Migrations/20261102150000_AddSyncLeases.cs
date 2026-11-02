using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackgroundTaskExecutor.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncLeases",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TaskName = table.Column<string>(maxLength: 400, nullable: false),
                    Profile = table.Column<string>(maxLength: 200, nullable: false),
                    Token = table.Column<Guid>(nullable: true),
                    Owner = table.Column<string>(maxLength: 200, nullable: true),
                    LeasedUntil = table.Column<DateTime>(nullable: true),
                    StartedAt = table.Column<DateTime>(nullable: true),
                    FinishedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLeases", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncLeases");
        }
    }
}
