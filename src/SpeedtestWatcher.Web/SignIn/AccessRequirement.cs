using Microsoft.AspNetCore.Authorization;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.SignIn;

public sealed record AccessRequirement(Access Required) : IAuthorizationRequirement;
