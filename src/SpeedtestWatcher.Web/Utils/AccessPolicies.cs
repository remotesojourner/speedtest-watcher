using Microsoft.AspNetCore.Authorization;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Web.Authorization;

namespace SpeedtestWatcher.Web.Utils;

public static class AccessPolicies
{
    public const string Read = "Read";

    public const string Full = "Full";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(Read, policy => policy.AddRequirements(new AccessRequirement(Access.ReadOnly)));
        options.AddPolicy(Full, policy => policy.AddRequirements(new AccessRequirement(Access.Full)));
        options.FallbackPolicy = options.GetPolicy(Full);
    }
}
