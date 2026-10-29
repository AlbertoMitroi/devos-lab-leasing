using System.Globalization;
using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.AppServices.EventStore;
using Leasing.AppServices.Ports;

namespace Leasing.AppServices.Flows.Plugins.Lease;

/// <summary>Stores the lease document, publishes the activation to other domains and queues the welcome notice.</summary>
public sealed class ActivateLeaseHandler(
    ILeaseDocuments leases,
    ILeaseContractPublisher publisher,
    INoticeOutbox outbox,
    TimeProvider clock) : IHandleServiceTask
{
    public async Task<string?> Handle(FlowContext context, CancellationToken cancellationToken)
    {
        var data = context.Data;
        var lease = new LeaseDocument
        {
            Id = context.FlowId,
            ContractNumber = data[FlowDataKeys.ContractNumber],
            ApplicationId = data.GetValueOrDefault(FlowDataKeys.ApplicationId, ""),
            ApplicantName = data.GetValueOrDefault(FlowDataKeys.ApplicantName, ""),
            CompanyNumber = data.GetValueOrDefault(FlowDataKeys.CompanyNumber, ""),
            AssetType = data.GetValueOrDefault(FlowDataKeys.AssetType, ""),
            MonthlyPayment = decimal.Parse(data.GetValueOrDefault(FlowDataKeys.MonthlyPayment, "0"), CultureInfo.InvariantCulture),
            TermMonths = int.Parse(data.GetValueOrDefault(FlowDataKeys.TermMonths, "0"), CultureInfo.InvariantCulture),
            SignedBy = data.GetValueOrDefault(FlowDataKeys.SignedBy, ""),
            ActivatedAt = clock.GetUtcNow(),
        };
        leases.Store(lease);

        await publisher.PublishActivated(
            new LeaseContractActivatedFact(lease.ContractNumber, lease.Id, lease.ApplicationId, lease.CompanyNumber, lease.MonthlyPayment, lease.TermMonths, lease.ActivatedAt),
            cancellationToken);
        await outbox.AddPendingNotice(context.FlowId, lease.ContractNumber, lease.SignedBy, "lease-welcome", cancellationToken);
        return null;
    }
}
