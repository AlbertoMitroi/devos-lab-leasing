using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.AppServices.Ports;

namespace Leasing.AppServices.Flows.Plugins.Assessment;

public sealed class RequestCreditCheckHandler(ICreditCheckGateway creditCheck) : IHandleServiceTask
{
    public async Task<string?> Handle(FlowContext context, CancellationToken cancellationToken)
    {
        await creditCheck.RequestCreditCheck(context.FlowId, context.Data.GetValueOrDefault(FlowDataKeys.CompanyNumber, ""), cancellationToken);
        return null;
    }
}
