using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class CurrentAccess : ICurrentAccess
{
    private readonly CircuitAccess _circuit;
    private readonly HttpCurrentAccess _request;

    public CurrentAccess(CircuitAccess circuit, HttpCurrentAccess request)
    {
        _circuit = circuit;
        _request = request;
    }

    public Access Level => _circuit.IsStarted ? _circuit.Level : _request.Level;
}
