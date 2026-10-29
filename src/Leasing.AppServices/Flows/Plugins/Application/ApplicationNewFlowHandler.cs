using Lab.Core.Workflow.EventFlow.Plugins;

namespace Leasing.AppServices.Flows.Plugins.Application;

public sealed class ApplicationNewFlowHandler : IHandleNewFlow
{
    public Task Initialize(FlowContext context, IReadOnlyDictionary<string, string> trigger, CancellationToken cancellationToken)
    {
        if (!trigger.ContainsKey(FlowDataKeys.ApplicationId))
        {
            context.SetData(FlowDataKeys.ApplicationId, context.FlowId.ToString());
        }

        return Task.CompletedTask;
    }
}
