using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddBufferbloat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "bufferbloat",
            table: "speedtests",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "latencyIdle",
            table: "speedtests",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "latencyLoaded",
            table: "speedtests",
            type: "REAL",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bufferbloat",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "latencyIdle",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "latencyLoaded",
            table: "speedtests");
    }
}
