using System.Globalization;
using Lab.Intake.Contracts;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using MassTransit;
using MediatR;

namespace Leasing.AppServices.Messaging.Kafka;

/// <summary>Consumes <c>Kafka:Topics:Applications</c>: every submitted application starts an Application flow.</summary>
public sealed class LeaseApplicationSubmittedConsumer(IMediator mediator) : IConsumer<LeaseApplicationSubmitted>
{
    public const string Trigger = "LeaseApplicationReceived";

    public Task Consume(ConsumeContext<LeaseApplicationSubmitted> context)
    {
        var m = context.Message;
        return mediator.Send(
            new StartFlowCommand(
                Trigger,
                Guid.Parse(m.ApplicationId),
                new Dictionary<string, string>
                {
                    [FlowDataKeys.ApplicationId] = m.ApplicationId,
                    [FlowDataKeys.ApplicantName] = m.ApplicantName,
                    [FlowDataKeys.CompanyNumber] = m.CompanyNumber,
                    [FlowDataKeys.AssetType] = m.AssetType,
                    [FlowDataKeys.AssetValue] = m.AssetValue.ToString(CultureInfo.InvariantCulture),
                    [FlowDataKeys.TermMonths] = m.TermMonths.ToString(CultureInfo.InvariantCulture),
                }),
            context.CancellationToken);
    }
}
