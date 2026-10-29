using Leasing.AppServices.Flows;
using Leasing.AppServices.Ports;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Leasing.AppServices.Messaging;

public sealed class BusCreditCheckGateway(ISendEndpointProvider sendEndpoints, IOptions<MessagingOptions> options) : ICreditCheckGateway
{
    public async Task RequestCreditCheck(Guid flowId, string companyNumber, CancellationToken cancellationToken)
    {
        var endpoint = await sendEndpoints.GetSendEndpoint(new Uri($"queue:{options.Value.Queues.CreditCheckRequests}"));
        await endpoint.Send(new CreditCheckRequested(flowId, companyNumber), cancellationToken);
    }
}

public sealed class BusNoticeOutbox(ISendEndpointProvider sendEndpoints, IOptions<MessagingOptions> options) : INoticeOutbox
{
    public async Task AddPendingNotice(Guid flowId, string contractNumber, string recipient, string template, CancellationToken cancellationToken)
    {
        var endpoint = await sendEndpoints.GetSendEndpoint(new Uri($"queue:{options.Value.Queues.NoticeOutbox}"));
        await endpoint.Send(new PendingNoticeAddedToOutbox(flowId, contractNumber, recipient, template), cancellationToken);
    }
}

public sealed class BusFlowChangeNotifier(ISendEndpointProvider sendEndpoints, IOptions<MessagingOptions> options) : IFlowChangeNotifier
{
    public async Task FlowChanged(Guid flowId, CancellationToken cancellationToken)
    {
        var endpoint = await sendEndpoints.GetSendEndpoint(new Uri($"queue:{options.Value.Queues.Projections}"));
        await endpoint.Send(new FlowEventsAppended(flowId), cancellationToken);
    }
}
