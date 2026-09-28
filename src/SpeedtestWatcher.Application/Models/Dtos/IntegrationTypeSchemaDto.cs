namespace SpeedtestWatcher.Application.Models.Dtos;

public class IntegrationTypeSchemaDto
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<IntegrationFieldSchemaDto> Fields { get; set; } = [];
}
