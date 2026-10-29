using Lab.Core.Workflow.EventFlow.Plugins;

namespace Leasing.AppServices.Flows.Plugins.Lease;

public sealed class PrepareContractHandler(TimeProvider clock) : IHandleServiceTask
{
    public Task<string?> Handle(FlowContext context, CancellationToken cancellationToken)
    {
        context.SetData("contractPreparedAt", clock.GetUtcNow().ToString("O"));
        return Task.FromResult<string?>(null);
    }
}
