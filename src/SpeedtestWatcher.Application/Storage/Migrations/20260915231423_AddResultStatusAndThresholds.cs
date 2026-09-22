using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddResultStatusAndThresholds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "healthy",
            table: "speedtests",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "status",
            table: "speedtests",
            type: "TEXT",
            nullable: false,
            defaultValue: "completed");

        migrationBuilder.AddColumn<double>(
            name: "thresholdDownload",
            table: "speedtests",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "thresholdPing",
            table: "speedtests",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "thresholdUpload",
            table: "speedtests",
            type: "REAL",
            nullable: true);

        migrationBuilder.Sql("UPDATE speedtests SET status = 'failed' WHERE error IS NOT NULL AND error <> ''");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "healthy",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "status",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "thresholdDownload",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "thresholdPing",
            table: "speedtests");

        migrationBuilder.DropColumn(
            name: "thresholdUpload",
            table: "speedtests");
    }
}
