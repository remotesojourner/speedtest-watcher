using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Web.Services;

public sealed class CurrentAccessService : ICurrentAccessService
{
    private readonly CircuitAccessService _circuit;
    private readonly HttpCurrentAccessService _request;

    public CurrentAccessService(CircuitAccessService circuit, HttpCurrentAccessService request)
    {
        _circuit = circuit;
        _request = request;
    }

    public Access Level => _circuit.IsStarted ? _circuit.Level : _request.Level;
}
