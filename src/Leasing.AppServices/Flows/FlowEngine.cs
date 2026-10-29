using Lab.Core.Transform.Bpmn;
using Lab.Core.Workflow.EventFlow;
using Lab.Core.Workflow.EventFlow.Plugins;
using Microsoft.Extensions.Logging;

namespace Leasing.AppServices.Flows;

/// <summary>
/// Runs flows: rehydrates the flow, asks the <see cref="EventFlowAnalyzer"/> what is open, calls the plugins,
/// appends the new events and starts the child flows whose processes are started by a raised fact or a reached status.
/// </summary>
public sealed class FlowEngine(
    ProcessCatalog catalog,
    PluginRegistry plugins,
    IFlowStore store,
    IFlowChangeNotifier notifier,
    EventFlowAnalyzer analyzer,
    ILogger<FlowEngine> logger)
{
    public async Task<IReadOnlyList<Guid>> StartFlows(string trigger, Guid? flowId, Guid? parentFlowId, IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken)
    {
        var started = new List<Guid>();
        foreach (var process in catalog.StartedBy(trigger))
        {
            var id = started.Count == 0 && flowId is not null ? flowId.Value : Guid.NewGuid();
            if (await store.Load(id, cancellationToken) is not null)
            {
                logger.LogInformation("Flow {FlowId} already exists, {Trigger} ignored", id, trigger);
                continue;
            }

            var flow = new FlowAggregate();
            var pending = new List<FlowEvent>();
            Record(flow, pending, new FlowStarted(id, process.ProcessId, parentFlowId, trigger, new Dictionary<string, string>(data)));
            if (plugins.NewFlow(process.ProcessId) is { } initializer)
            {
                var context = new FlowContext(flow, null);
                await initializer.Initialize(context, data, cancellationToken);
                RecordAll(flow, pending, context.NewEvents);
            }

            await Advance(process, flow, pending, expectedVersion: null, userId: null, cancellationToken);
            started.Add(id);
        }

        return started;
    }

    public async Task<FlowView> CompleteUserTask<TCommand>(string taskId, TCommand command, string? userId, CancellationToken cancellationToken)
        where TCommand : IUserTaskCommand
    {
        var (process, flow) = await Load(command.FlowId, cancellationToken);
        var analysis = analyzer.Analyze(process, flow);
        if (!analysis.OpenTasks.Any(t => t.TaskId == taskId && t.Kind == NodeKind.UserTask))
        {
            throw new FlowConflictException($"User task '{taskId}' is not open on flow {flow.Id}.");
        }

        var version = flow.Version;
        var context = new FlowContext(flow, userId);
        var outcome = await plugins.UserTask<TCommand>(taskId).Handle(command, context, cancellationToken);
        var pending = new List<FlowEvent>();
        RecordAll(flow, pending, context.NewEvents);
        Record(flow, pending, new TaskCompleted(flow.Id, taskId, outcome, userId));
        return await Advance(process, flow, pending, version, userId, cancellationToken);
    }

    public async Task<FlowView> ExecuteAction(Guid flowId, string actionName, IReadOnlyDictionary<string, string> parameters, string? userId, CancellationToken cancellationToken)
    {
        var (process, flow) = await Load(flowId, cancellationToken);
        if (!analyzer.Analyze(process, flow).AvailableActions.Contains(actionName, StringComparer.Ordinal))
        {
            throw new FlowConflictException($"Action '{actionName}' is not available on flow {flow.Id}.");
        }

        var version = flow.Version;
        var pending = new List<FlowEvent>();
        if (parameters.Count > 0)
        {
            Record(flow, pending, new FlowDataUpdated(flow.Id, new Dictionary<string, string>(parameters)));
        }

        var context = new FlowContext(flow, userId);
        await plugins.Action(actionName).Handle(context, cancellationToken);
        RecordAll(flow, pending, context.NewEvents);
        Record(flow, pending, new ActionExecuted(flow.Id, actionName, userId));
        return await Advance(process, flow, pending, version, userId, cancellationToken);
    }

    public async Task<FlowView> UpdateData(Guid flowId, IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken)
    {
        var (process, flow) = await Load(flowId, cancellationToken);
        var version = flow.Version;
        var pending = new List<FlowEvent>();
        Record(flow, pending, new FlowDataUpdated(flow.Id, new Dictionary<string, string>(data)));
        return await Advance(process, flow, pending, version, null, cancellationToken);
    }

    public async Task<FlowView> Get(Guid flowId, CancellationToken cancellationToken)
    {
        var (process, flow) = await Load(flowId, cancellationToken);
        return FlowView.From(process, flow, analyzer.Analyze(process, flow));
    }

    private async Task<FlowView> Advance(ProcessDefinition process, FlowAggregate flow, List<FlowEvent> pending, int? expectedVersion, string? userId, CancellationToken cancellationToken)
    {
        while (analyzer.Analyze(process, flow).OpenTasks.FirstOrDefault(t => t.Kind == NodeKind.ServiceTask) is { } serviceTask)
        {
            var context = new FlowContext(flow, userId);
            var outcome = await plugins.ServiceTask(serviceTask.TaskId).Handle(context, cancellationToken);
            RecordAll(flow, pending, context.NewEvents);
            Record(flow, pending, new TaskCompleted(flow.Id, serviceTask.TaskId, outcome, null));
        }

        var analysis = analyzer.Analyze(process, flow);
        if (analysis.Status != flow.Status)
        {
            Record(flow, pending, new FlowStatusChanged(flow.Id, analysis.Status));
        }

        if (expectedVersion is null)
        {
            await store.StartStream(flow.Id, pending, cancellationToken);
        }
        else
        {
            await store.Append(flow.Id, expectedVersion.Value, pending, cancellationToken);
        }

        flow.Version = (expectedVersion ?? 0) + pending.Count;
        await notifier.FlowChanged(flow.Id, cancellationToken);

        var triggers = pending.OfType<FlowFactRaised>().Select(f => f.Fact)
            .Concat(pending.OfType<FlowStatusChanged>().Select(s => s.Status));
        foreach (var trigger in triggers)
        {
            await StartFlows(trigger, null, flow.Id, flow.Data, cancellationToken);
        }

        return FlowView.From(process, flow, analysis);
    }

    private async Task<(ProcessDefinition Process, FlowAggregate Flow)> Load(Guid flowId, CancellationToken cancellationToken)
    {
        var flow = await store.Load(flowId, cancellationToken) ?? throw new FlowNotFoundException(flowId);
        return (catalog.Get(flow.ProcessId), flow);
    }

    private static void RecordAll(FlowAggregate flow, List<FlowEvent> pending, IEnumerable<FlowEvent> events)
    {
        foreach (var e in events)
        {
            Record(flow, pending, e);
        }
    }

    private static void Record(FlowAggregate flow, List<FlowEvent> pending, FlowEvent e)
    {
        pending.Add(e);
        switch (e)
        {
            case FlowStarted x: flow.Apply(x); break;
            case TaskCompleted x: flow.Apply(x); break;
            case ActionExecuted x: flow.Apply(x); break;
            case FlowDataUpdated x: flow.Apply(x); break;
            case FlowStatusChanged x: flow.Apply(x); break;
            case FlowFactRaised x: flow.Apply(x); break;
        }
    }
}
