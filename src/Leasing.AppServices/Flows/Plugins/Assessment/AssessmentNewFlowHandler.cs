using Lab.Core.Workflow.EventFlow.Plugins;

namespace Leasing.AppServices.Flows.Plugins.Assessment;

public sealed class AssessmentNewFlowHandler : IHandleNewFlow
{
    public Task Initialize(FlowContext context, IReadOnlyDictionary<string, string> trigger, CancellationToken cancellationToken)
    {
        context.SetData(FlowDataKeys.RiskGrade, "pending");
        return Task.CompletedTask;
    }
}
