using Microsoft.AspNetCore.Authorization;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed record AccessRequirement(Access Required) : IAuthorizationRequirement;
