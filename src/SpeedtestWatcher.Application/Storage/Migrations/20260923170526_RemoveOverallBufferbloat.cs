using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class RemoveOverallBufferbloat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bufferbloat",
            table: "speedtests");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "bufferbloat",
            table: "speedtests",
            type: "REAL",
            nullable: true);
    }
}
