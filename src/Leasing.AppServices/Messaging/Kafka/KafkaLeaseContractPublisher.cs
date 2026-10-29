using Lab.Leasing.Contracts;
using Leasing.AppServices.Ports;
using MassTransit;

namespace Leasing.AppServices.Messaging.Kafka;

/// <summary>Produces to <c>Kafka:Topics:Contracts</c>; consumed by other domains, outside this system.</summary>
public sealed class KafkaLeaseContractPublisher(ITopicProducer<string, LeaseContractActivated> producer) : ILeaseContractPublisher
{
    public Task PublishActivated(LeaseContractActivatedFact fact, CancellationToken cancellationToken) =>
        producer.Produce(
            fact.ContractNumber,
            new LeaseContractActivated
            {
                ContractNumber = fact.ContractNumber,
                LeaseFlowId = fact.LeaseFlowId.ToString(),
                ApplicationId = fact.ApplicationId,
                CompanyNumber = fact.CompanyNumber,
                MonthlyPayment = (double)fact.MonthlyPayment,
                TermMonths = fact.TermMonths,
                ActivatedAtUnixMs = fact.ActivatedAt.ToUnixTimeMilliseconds(),
            },
            cancellationToken);
}

/// <summary>Used when <c>Kafka:Enabled</c> is false (sandbox).</summary>
public sealed class DisabledLeaseContractPublisher : ILeaseContractPublisher
{
    public Task PublishActivated(LeaseContractActivatedFact fact, CancellationToken cancellationToken) => Task.CompletedTask;
}
