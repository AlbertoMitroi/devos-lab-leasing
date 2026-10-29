using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.AppServices.Flows.Commands;

namespace Leasing.AppServices.Flows.Plugins.Application;

public sealed class ReviewApplicationHandler : IHandleUserTaskCommand<ReviewApplicationCommand>
{
    public Task<string?> Handle(ReviewApplicationCommand command, FlowContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Comment))
        {
            context.SetData(FlowDataKeys.ReviewComment, command.Comment);
        }

        return Task.FromResult<string?>(command.Approve ? "Approved" : "Declined");
    }
}
