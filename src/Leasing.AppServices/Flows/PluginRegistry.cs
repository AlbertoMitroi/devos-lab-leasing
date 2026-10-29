using System.Reflection;
using Lab.Core.Workflow.EventFlow.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Leasing.AppServices.Flows;

/// <summary>
/// Finds Flow Engine plugins by the naming conventions of <see cref="FlowPluginConventions"/>:
/// <c>{TaskId}Handler</c>, <c>{ActionName}ActionHandler</c>, <c>{ProcessId}NewFlowHandler</c>.
/// </summary>
public sealed class PluginRegistry(IServiceProvider services)
{
    private static readonly Type[] PluginTypes = typeof(PluginRegistry).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(IsPluginInterface))
        .OrderBy(t => t.FullName, StringComparer.Ordinal)
        .ToArray();

    private static readonly Dictionary<string, Type> Tasks = PluginTypes
        .Where(t => Implements(t, typeof(IHandleServiceTask)) || Implements(t, typeof(IHandleUserTaskCommand<>)))
        .ToDictionary(FlowPluginConventions.TaskIdOf, StringComparer.Ordinal);

    private static readonly Dictionary<string, Type> Actions = PluginTypes
        .Where(t => Implements(t, typeof(IHandleAction)))
        .ToDictionary(FlowPluginConventions.ActionNameOf, StringComparer.Ordinal);

    private static readonly Dictionary<string, Type> NewFlows = PluginTypes
        .Where(t => Implements(t, typeof(IHandleNewFlow)))
        .ToDictionary(FlowPluginConventions.ProcessIdOf, StringComparer.Ordinal);

    public static IReadOnlyCollection<string> TaskIds => Tasks.Keys;

    public static IReadOnlyCollection<string> ActionNames => Actions.Keys;

    public static IReadOnlyCollection<string> NewFlowProcessIds => NewFlows.Keys;

    public static void Register(IServiceCollection serviceCollection)
    {
        foreach (var type in PluginTypes)
        {
            serviceCollection.AddScoped(type);
        }
    }

    public IHandleServiceTask ServiceTask(string taskId) => (IHandleServiceTask)Resolve(Tasks, taskId, "service task");

    public IHandleUserTaskCommand<TCommand> UserTask<TCommand>(string taskId)
        where TCommand : IUserTaskCommand =>
        Resolve(Tasks, taskId, "user task") as IHandleUserTaskCommand<TCommand>
        ?? throw new InvalidOperationException($"Plugin for user task '{taskId}' does not handle {typeof(TCommand).Name}.");

    public IHandleAction Action(string actionName) => (IHandleAction)Resolve(Actions, actionName, "action");

    public IHandleNewFlow? NewFlow(string processId) =>
        NewFlows.TryGetValue(processId, out var type) ? (IHandleNewFlow)services.GetRequiredService(type) : null;

    private object Resolve(Dictionary<string, Type> map, string key, string kind) =>
        map.TryGetValue(key, out var type)
            ? services.GetRequiredService(type)
            : throw new InvalidOperationException($"No plugin for {kind} '{key}'.");

    private static bool IsPluginInterface(Type i) =>
        i == typeof(IHandleServiceTask) || i == typeof(IHandleAction) || i == typeof(IHandleNewFlow)
        || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleUserTaskCommand<>));

    private static bool Implements(Type type, Type contract) =>
        type.GetInterfaces().Any(i => i == contract || (i.IsGenericType && i.GetGenericTypeDefinition() == contract));
}
