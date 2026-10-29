using System.Globalization;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using MassTransit;
using MediatR;

namespace Leasing.AppServices.Messaging.Consumers;

public sealed class CreditCheckResponseConsumer(IMediator mediator) : IConsumer<CreditCheckResponse>
{
    public Task Consume(ConsumeContext<CreditCheckResponse> context) =>
        mediator.Send(
            new UpdateFlowDataCommand(
                context.Message.FlowId,
                new Dictionary<string, string> { [FlowDataKeys.CreditScore] = context.Message.CreditScore.ToString(CultureInfo.InvariantCulture) }),
            context.CancellationToken);
}
