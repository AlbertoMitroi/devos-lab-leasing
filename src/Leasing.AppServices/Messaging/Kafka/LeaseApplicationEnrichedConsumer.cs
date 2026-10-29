using System.Globalization;
using Lab.Intake.Contracts;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using MassTransit;
using MediatR;

namespace Leasing.AppServices.Messaging.Kafka;

/// <summary>Consumes <c>Kafka:Topics:Enrichment</c>: adds company data to the Application flow of the application.</summary>
public sealed class LeaseApplicationEnrichedConsumer(IMediator mediator) : IConsumer<LeaseApplicationEnriched>
{
    public Task Consume(ConsumeContext<LeaseApplicationEnriched> context) =>
        mediator.Send(
            new UpdateFlowDataCommand(
                Guid.Parse(context.Message.ApplicationId),
                new Dictionary<string, string>
                {
                    [FlowDataKeys.Sector] = context.Message.Sector,
                    [FlowDataKeys.YearsTrading] = context.Message.YearsTrading.ToString(CultureInfo.InvariantCulture),
                }),
            context.CancellationToken);
}
