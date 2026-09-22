using Microsoft.EntityFrameworkCore.Migrations;

namespace SpeedtestWatcher.Application.Storage.Migrations;

public partial class AddHealthyAgainToggle : Migration
{
    private const string TypesWithTheToggle = "'apprise', 'discord', 'gotify', 'ntfy', 'pushover', 'telegram', 'webhook'";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            UPDATE integration_data
            SET data = json_set(data, '$.send_healthy_again', json(
                CASE
                    WHEN json_type(data, '$.send_unhealthy') = 'false' OR lower(json_extract(data, '$.send_unhealthy')) = 'false' THEN 'false'
                    ELSE 'true'
                END))
            WHERE name IN ({TypesWithTheToggle})
              AND json_valid(data)
              AND json_type(data) = 'object'
              AND json_type(data, '$.send_healthy_again') IS NULL
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            UPDATE integration_data
            SET data = json_remove(data, '$.send_healthy_again')
            WHERE name IN ({TypesWithTheToggle})
              AND json_valid(data)
            """);
    }
}
