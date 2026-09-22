using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddDataUsedAndPacketLoss : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "downloadBytes",
            table: "speedtests",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "packetLoss",
            table: "speedtests",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "publicIp",
            table: "speedtests",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "uploadBytes",
            table: "speedtests",
            type: "INTEGER",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "downloadBytes",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "packetLoss",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "publicIp",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "uploadBytes",
            table: "speedtests");
    }
}
