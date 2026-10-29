using Leasing.AppServices.Projections;
using MassTransit;

namespace Leasing.AppServices.Messaging.Consumers;

public sealed class FlowEventsAppendedConsumer(FlowProjectionService projections) : IConsumer<FlowEventsAppended>
{
    public Task Consume(ConsumeContext<FlowEventsAppended> context) =>
        projections.Project(context.Message.FlowId, context.CancellationToken);
}
