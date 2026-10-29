using Lab.Core.Workflow.EventFlow;
using Marten.Events.Projections;

namespace Leasing.AppServices.EventStore.Projections;

public sealed class PortfolioSummary
{
    public const string DocumentId = "portfolio";

    public string Id { get; set; } = DocumentId;

    public Dictionary<string, int> FlowsByStatus { get; set; } = [];

    public int ActiveLeases { get; set; }
}

/// <summary>Async projection across all flow streams, run by the Marten async daemon.</summary>
public sealed class PortfolioSummaryProjection : MultiStreamProjection<PortfolioSummary, string>
{
    public const string LeaseActive = "LeaseActive";

    public PortfolioSummaryProjection()
    {
        Identity<FlowStatusChanged>(_ => PortfolioSummary.DocumentId);
    }

    public void Apply(FlowStatusChanged e, PortfolioSummary summary)
    {
        summary.FlowsByStatus[e.Status] = summary.FlowsByStatus.GetValueOrDefault(e.Status) + 1;
        if (e.Status == LeaseActive)
        {
            summary.ActiveLeases++;
        }
    }
}
