using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.AppServices.Flows.Commands;

namespace Leasing.AppServices.Flows.Plugins.Assessment;

public sealed class AssessRiskHandler : IHandleUserTaskCommand<AssessRiskCommand>
{
    private static readonly string[] AcceptableGrades = ["A", "B", "C"];

    public Task<string?> Handle(AssessRiskCommand command, FlowContext context, CancellationToken cancellationToken)
    {
        context.SetData(FlowDataKeys.RiskGrade, command.RiskGrade);
        return Task.FromResult<string?>(AcceptableGrades.Contains(command.RiskGrade) ? "Acceptable" : "Unacceptable");
    }
}
