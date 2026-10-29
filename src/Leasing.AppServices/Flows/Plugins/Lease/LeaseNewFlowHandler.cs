using Lab.Core.Workflow.EventFlow.Plugins;

namespace Leasing.AppServices.Flows.Plugins.Lease;

public sealed class LeaseNewFlowHandler : IHandleNewFlow
{
    public Task Initialize(FlowContext context, IReadOnlyDictionary<string, string> trigger, CancellationToken cancellationToken)
    {
        context.SetData(FlowDataKeys.ContractNumber, "LC-" + context.FlowId.ToString("N")[..8].ToUpperInvariant());
        return Task.CompletedTask;
    }
}
