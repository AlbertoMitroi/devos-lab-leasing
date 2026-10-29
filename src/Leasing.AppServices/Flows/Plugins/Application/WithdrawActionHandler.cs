using Lab.Core.Workflow.EventFlow;
using Lab.Core.Workflow.EventFlow.Plugins;

namespace Leasing.AppServices.Flows.Plugins.Application;

public sealed class WithdrawActionHandler : IHandleAction
{
    public Task Handle(FlowContext context, CancellationToken cancellationToken)
    {
        context.SetData(FlowDataKeys.WithdrawnBy, context.UserId ?? "unknown");
        context.ChangeStatus(FlowStatuses.Withdrawn);
        context.RaiseFact("ApplicationWithdrawn");
        return Task.CompletedTask;
    }
}
