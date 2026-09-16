using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedtestWatcher.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "config",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "integration_data",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    displayName = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Untitled"),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    data = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    lastActivity = table.Column<DateTime>(type: "TEXT", nullable: true),
                    activityFailed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_data", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recommendations",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ping = table.Column<int>(type: "INTEGER", nullable: false),
                    download = table.Column<double>(type: "REAL", nullable: false),
                    upload = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "speedtests",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    serverId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    serverName = table.Column<string>(type: "TEXT", nullable: true),
                    serverHost = table.Column<string>(type: "TEXT", nullable: true),
                    ping = table.Column<int>(type: "INTEGER", nullable: false),
                    jitter = table.Column<double>(type: "REAL", nullable: true),
                    download = table.Column<double>(type: "REAL", nullable: false),
                    upload = table.Column<double>(type: "REAL", nullable: false),
                    error = table.Column<string>(type: "TEXT", nullable: true),
                    type = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "auto"),
                    resultId = table.Column<string>(type: "TEXT", nullable: true),
                    time = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    created = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_speedtests", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "config");

            migrationBuilder.DropTable(
                name: "integration_data");

            migrationBuilder.DropTable(
                name: "recommendations");

            migrationBuilder.DropTable(
                name: "speedtests");
        }
    }
}
