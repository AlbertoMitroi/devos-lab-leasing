namespace Leasing.AppServices.Ports;

/// <summary>Asks the credit bureau for a credit check; the answer comes back as a <c>CreditCheckResponse</c> bus message.</summary>
public interface ICreditCheckGateway
{
    Task RequestCreditCheck(Guid flowId, string companyNumber, CancellationToken cancellationToken);
}

/// <summary>Hands a notice to the notice outbox service, which renders and sends it.</summary>
public interface INoticeOutbox
{
    Task AddPendingNotice(Guid flowId, string contractNumber, string recipient, string template, CancellationToken cancellationToken);
}

/// <summary>Publishes leasing facts to other domains.</summary>
public interface ILeaseContractPublisher
{
    Task PublishActivated(LeaseContractActivatedFact fact, CancellationToken cancellationToken);
}

public sealed record LeaseContractActivatedFact(string ContractNumber, Guid LeaseFlowId, string ApplicationId, string CompanyNumber, decimal MonthlyPayment, int TermMonths, DateTimeOffset ActivatedAt);

/// <summary>Documents of active leases.</summary>
public interface ILeaseDocuments
{
    void Store(EventStore.LeaseDocument lease);
}
