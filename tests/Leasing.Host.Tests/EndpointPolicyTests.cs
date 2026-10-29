using System.Reflection;
using FluentAssertions;
using Leasing.Host.Authorization;
using Leasing.Host.Controllers.V1;
using Leasing.Host.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Leasing.Host.Tests;

public class EndpointPolicyTests
{
    private static readonly Type[] Controllers = typeof(LeasingControllerBase).Assembly.GetTypes()
        .Where(t => t is { IsAbstract: false } && t.IsSubclassOf(typeof(LeasingControllerBase)))
        .ToArray();

    [Fact]
    public void Every_controller_is_under_v1()
    {
        Controllers.Should().NotBeEmpty();
        Controllers.Select(c => c.GetCustomAttribute<RouteAttribute>()!.Template).Should().OnlyContain(t => t.StartsWith("v1/"));
    }

    [Fact]
    public void Every_action_has_a_policy_and_writes_need_the_write_or_admin_policy()
    {
        foreach (var action in Controllers.SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
        {
            var policy = action.GetCustomAttribute<AuthorizeAttribute>()?.Policy ?? action.DeclaringType!.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
            policy.Should().NotBeNull($"{action.DeclaringType!.Name}.{action.Name} needs a policy");
            if (action.GetCustomAttributes<HttpMethodAttribute>().Any(a => a.HttpMethods.Contains("POST")))
            {
                policy.Should().BeOneOf(Policies.DefaultWrite, Policies.Admin);
            }
        }
    }

    [Fact]
    public void Hub_requires_the_read_policy()
    {
        typeof(LeasingHub).GetCustomAttribute<AuthorizeAttribute>()!.Policy.Should().Be(Policies.DefaultRead);
    }
}
