using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddBufferbloatByDirection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "bufferbloatDown",
            table: "speedtests",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "bufferbloatUp",
            table: "speedtests",
            type: "REAL",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bufferbloatDown",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "bufferbloatUp",
            table: "speedtests");
    }
}
