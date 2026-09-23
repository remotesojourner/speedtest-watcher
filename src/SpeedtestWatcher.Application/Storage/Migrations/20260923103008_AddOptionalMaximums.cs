using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddOptionalMaximums : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "thresholdBufferbloat",
            table: "speedtests",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "thresholdPacketLoss",
            table: "speedtests",
            type: "REAL",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "thresholdBufferbloat",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "thresholdPacketLoss",
            table: "speedtests");
    }
}
