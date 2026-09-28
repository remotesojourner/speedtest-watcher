namespace SpeedtestWatcher.Application.Models.Dtos;

public class ConsistencyDto
{
    public ConsistencyItemDto Download { get; set; } = new();
    public ConsistencyItemDto Upload { get; set; } = new();
    public PingConsistencyDto Ping { get; set; } = new();
}
