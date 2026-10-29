using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.AppServices.Flows.Commands;

namespace Leasing.AppServices.Flows.Plugins.Lease;

public sealed class SignContractHandler : IHandleUserTaskCommand<SignContractCommand>
{
    public Task<string?> Handle(SignContractCommand command, FlowContext context, CancellationToken cancellationToken)
    {
        context.SetData(FlowDataKeys.SignedBy, command.SignedBy);
        return Task.FromResult<string?>(null);
    }
}
