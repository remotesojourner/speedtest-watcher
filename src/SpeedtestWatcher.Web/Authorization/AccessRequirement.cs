using Microsoft.AspNetCore.Authorization;
using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Web.Authorization;

public sealed record AccessRequirement(Access Required) : IAuthorizationRequirement;
