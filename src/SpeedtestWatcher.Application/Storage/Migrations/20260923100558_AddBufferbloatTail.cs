using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddBufferbloatTail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "latencyLoadedTail",
            table: "speedtests",
            type: "REAL",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "latencyLoadedTail",
            table: "speedtests");
    }
}
