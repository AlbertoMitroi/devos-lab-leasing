using Lab.Core.Workflow.EventFlow;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Ports;
using Marten;

namespace Leasing.AppServices.EventStore;

/// <summary>Flow streams in the Marten event store; the <see cref="FlowAggregate"/> snapshot is maintained inline.</summary>
public sealed class MartenFlowStore(IDocumentSession session) : IFlowStore, ILeaseDocuments
{
    public Task<FlowAggregate?> Load(Guid flowId, CancellationToken cancellationToken) =>
        session.LoadAsync<FlowAggregate>(flowId, cancellationToken);

    public async Task StartStream(Guid flowId, IReadOnlyList<FlowEvent> events, CancellationToken cancellationToken)
    {
        session.Events.StartStream<FlowAggregate>(flowId, events.Cast<object>().ToArray());
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task Append(Guid flowId, int expectedVersion, IReadOnlyList<FlowEvent> events, CancellationToken cancellationToken)
    {
        // Marten expects the version of the stream after the append.
        session.Events.Append(flowId, expectedVersion + events.Count, events.Cast<object>().ToArray());
        await session.SaveChangesAsync(cancellationToken);
    }

    public void Store(LeaseDocument lease) => session.Store(lease);
}
