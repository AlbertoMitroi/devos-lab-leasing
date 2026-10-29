using Lab.Core.Workflow.EventFlow;
using Leasing.AppServices.EventStore;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Ports;
using Leasing.Pricing;

namespace Leasing.AppServices.Tests;

/// <summary>Event store in memory with the same optimistic version check as the Marten store.</summary>
public sealed class InMemoryFlowStore : IFlowStore
{
    private readonly Dictionary<Guid, List<FlowEvent>> _streams = [];

    public IReadOnlyDictionary<Guid, List<FlowEvent>> Streams => _streams;

    public Task<FlowAggregate?> Load(Guid flowId, CancellationToken cancellationToken)
    {
        if (!_streams.TryGetValue(flowId, out var events))
        {
            return Task.FromResult<FlowAggregate?>(null);
        }

        var flow = new FlowAggregate();
        foreach (dynamic e in events)
        {
            flow.Apply(e);
        }

        flow.Version = events.Count;
        return Task.FromResult<FlowAggregate?>(flow);
    }

    public Task StartStream(Guid flowId, IReadOnlyList<FlowEvent> events, CancellationToken cancellationToken)
    {
        if (!_streams.TryAdd(flowId, [.. events]))
        {
            throw new InvalidOperationException($"Stream {flowId} exists.");
        }

        return Task.CompletedTask;
    }

    public Task Append(Guid flowId, int expectedVersion, IReadOnlyList<FlowEvent> events, CancellationToken cancellationToken)
    {
        var stream = _streams[flowId];
        if (stream.Count != expectedVersion)
        {
            throw new InvalidOperationException($"Stream {flowId} is at version {stream.Count}, expected {expectedVersion}.");
        }

        stream.AddRange(events);
        return Task.CompletedTask;
    }

    public FlowAggregate Single(string processId) =>
        _streams.Keys.Select(id => Load(id, default).Result!).Single(f => f.ProcessId == processId);
}

public sealed class Recorder : IFlowChangeNotifier, ICreditCheckGateway, INoticeOutbox, ILeaseContractPublisher, ILeaseDocuments, IQuotePricing
{
    public List<Guid> ChangedFlows { get; } = [];

    public List<(Guid FlowId, string CompanyNumber)> CreditChecks { get; } = [];

    public List<(string ContractNumber, string Recipient, string Template)> Notices { get; } = [];

    public List<LeaseContractActivatedFact> Activations { get; } = [];

    public List<LeaseDocument> Leases { get; } = [];

    public List<QuotePricingRequest> PricingRequests { get; } = [];

    public List<(Guid QuoteId, int TermMonths)> Repricings { get; } = [];

    public Guid PricingQuoteId { get; } = Guid.NewGuid();

    public Task FlowChanged(Guid flowId, CancellationToken cancellationToken)
    {
        ChangedFlows.Add(flowId);
        return Task.CompletedTask;
    }

    public Task RequestCreditCheck(Guid flowId, string companyNumber, CancellationToken cancellationToken)
    {
        CreditChecks.Add((flowId, companyNumber));
        return Task.CompletedTask;
    }

    public Task AddPendingNotice(Guid flowId, string contractNumber, string recipient, string template, CancellationToken cancellationToken)
    {
        Notices.Add((contractNumber, recipient, template));
        return Task.CompletedTask;
    }

    public Task PublishActivated(LeaseContractActivatedFact fact, CancellationToken cancellationToken)
    {
        Activations.Add(fact);
        return Task.CompletedTask;
    }

    public void Store(LeaseDocument lease) => Leases.Add(lease);

    public Task<QuotePricingResult> PriceAsync(QuotePricingRequest request, CancellationToken cancellationToken)
    {
        PricingRequests.Add(request);
        return Task.FromResult(new QuotePricingResult(PricingQuoteId, 812.34m, 5.9m, 25m, request.TermMonths));
    }

    public Task<QuotePricingResult> RepriceAsync(Guid pricingQuoteId, int termMonths, CancellationToken cancellationToken)
    {
        Repricings.Add((pricingQuoteId, termMonths));
        return Task.FromResult(new QuotePricingResult(pricingQuoteId, 640.00m, 5.9m, 18m, termMonths));
    }
}
