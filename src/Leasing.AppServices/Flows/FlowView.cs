using Lab.Core.Transform.Bpmn;
using Lab.Core.Workflow.EventFlow;

namespace Leasing.AppServices.Flows;

public sealed record FlowView(
    Guid FlowId,
    string ProcessId,
    string Status,
    Guid? ParentFlowId,
    IReadOnlyList<OpenTask> OpenTasks,
    IReadOnlyList<string> AvailableActions,
    IReadOnlyDictionary<string, string> Data,
    int Version)
{
    public static FlowView From(ProcessDefinition process, FlowAggregate flow, FlowAnalysis analysis) =>
        new(flow.Id, process.ProcessId, analysis.Status, flow.ParentFlowId, analysis.OpenTasks, analysis.AvailableActions, new Dictionary<string, string>(flow.Data), flow.Version);
}

public sealed class FlowNotFoundException(Guid flowId) : Exception($"Flow {flowId} not found.");

public sealed class FlowConflictException(string message) : Exception(message);
