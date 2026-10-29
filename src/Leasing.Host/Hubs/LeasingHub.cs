using Leasing.Host.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Leasing.Host.Hubs;

/// <summary>
/// Push channel to the web app. Server to client methods: <c>flowUpdated</c> (to all) and <c>taskAssigned</c>
/// (to the members of the work team of an open user task), sent through <c>IMessageHubUtility</c>.
/// </summary>
[Authorize(Policy = Policies.DefaultRead)]
public sealed class LeasingHub : Hub
{
    public const string Route = "/hubs/leasing";
}
