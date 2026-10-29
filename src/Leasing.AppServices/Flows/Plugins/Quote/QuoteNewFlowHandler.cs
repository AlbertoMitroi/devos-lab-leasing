using Lab.Core.Workflow.EventFlow.Plugins;

namespace Leasing.AppServices.Flows.Plugins.Quote;

public sealed class QuoteNewFlowHandler : IHandleNewFlow
{
    public const string DefaultProductCode = "EQ-STD";

    public Task Initialize(FlowContext context, IReadOnlyDictionary<string, string> trigger, CancellationToken cancellationToken)
    {
        if (!trigger.ContainsKey(FlowDataKeys.ProductCode))
        {
            context.SetData(FlowDataKeys.ProductCode, DefaultProductCode);
        }

        return Task.CompletedTask;
    }
}
