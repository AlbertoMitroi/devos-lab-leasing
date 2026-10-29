using Lab.Core.Workflow.EventFlow;

namespace Leasing.AppServices.Flows;

/// <summary>Event store of flows: one stream per flow.</summary>
public interface IFlowStore
{
    Task<FlowAggregate?> Load(Guid flowId, CancellationToken cancellationToken);

    Task StartStream(Guid flowId, IReadOnlyList<FlowEvent> events, CancellationToken cancellationToken);

    /// <param name="expectedVersion">Version of the stream before <paramref name="events"/>; a different version is a concurrency conflict.</param>
    Task Append(Guid flowId, int expectedVersion, IReadOnlyList<FlowEvent> events, CancellationToken cancellationToken);
}

/// <summary>Told after events were appended, so the read models of the flow are rebuilt.</summary>
public interface IFlowChangeNotifier
{
    Task FlowChanged(Guid flowId, CancellationToken cancellationToken);
}
