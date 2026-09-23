using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddConnectionMonitoring : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "outages",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                startedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                endedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "probe_rounds",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                at = table.Column<DateTime>(type: "TEXT", nullable: false),
                passed = table.Column<bool>(type: "INTEGER", nullable: false),
                answered = table.Column<int>(type: "INTEGER", nullable: false),
                asked = table.Column<int>(type: "INTEGER", nullable: false),
                fastestMs = table.Column<double>(type: "REAL", nullable: true),
                duringTest = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_probe_rounds", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "watch_sessions",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                startedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                lastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_watch_sessions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_outages_startedAt",
            table: "outages",
            column: "startedAt");

        migrationBuilder.CreateIndex(
            name: "IX_probe_rounds_at",
            table: "probe_rounds",
            column: "at");

        migrationBuilder.CreateIndex(
            name: "IX_watch_sessions_lastSeenAt",
            table: "watch_sessions",
            column: "lastSeenAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "outages");

        migrationBuilder.DropTable(
            name: "probe_rounds");

        migrationBuilder.DropTable(
            name: "watch_sessions");
    }
}
