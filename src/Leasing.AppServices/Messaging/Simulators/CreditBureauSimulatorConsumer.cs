using MassTransit;
using Microsoft.Extensions.Options;

namespace Leasing.AppServices.Messaging.Simulators;

/// <summary>
/// Local and Docker stand-in for the credit bureau adapter (<c>Messaging:CreditBureauSimulator</c>):
/// answers every credit check request with a score derived from the company number.
/// </summary>
public sealed class CreditBureauSimulatorConsumer(IOptions<MessagingOptions> options) : IConsumer<CreditCheckRequested>
{
    public async Task Consume(ConsumeContext<CreditCheckRequested> context)
    {
        var score = 300 + (Math.Abs(context.Message.CompanyNumber.GetHashCode(StringComparison.Ordinal)) % 550);
        var endpoint = await context.GetSendEndpoint(new Uri($"queue:{options.Value.Queues.CreditCheckResponses}"));
        await endpoint.Send(new CreditCheckResponse(context.Message.FlowId, score), context.CancellationToken);
    }
}
