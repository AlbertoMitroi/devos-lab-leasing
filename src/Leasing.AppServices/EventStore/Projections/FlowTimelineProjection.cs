using Lab.Core.Workflow.EventFlow;
using Marten.Events.Aggregation;

namespace Leasing.AppServices.EventStore.Projections;

public sealed class FlowTimeline
{
    public Guid Id { get; set; }

    public string ProcessId { get; set; } = "";

    public List<string> Entries { get; set; } = [];
}

/// <summary>Inline projection: one timeline document per flow stream.</summary>
public sealed class FlowTimelineProjection : SingleStreamProjection<FlowTimeline, Guid>
{
    public FlowTimeline Create(FlowStarted e) =>
        new() { Id = e.FlowId, ProcessId = e.ProcessId, Entries = [$"started by {e.TriggeredBy}"] };

    public void Apply(TaskCompleted e, FlowTimeline timeline) =>
        timeline.Entries.Add(e.Outcome is null ? $"{e.TaskId} completed" : $"{e.TaskId} completed: {e.Outcome}");

    public void Apply(ActionExecuted e, FlowTimeline timeline) => timeline.Entries.Add($"action {e.ActionName}");

    public void Apply(FlowStatusChanged e, FlowTimeline timeline) => timeline.Entries.Add($"status {e.Status}");
}
