using FluentAssertions;
using Lab.Core.Transform.Bpmn;
using Lab.Core.Workflow.EventFlow;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using Leasing.AppServices.Messaging.Kafka;
using Leasing.AppServices.Ports;
using Leasing.Pricing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Leasing.AppServices.Tests;

public class FlowEngineTests
{
    private readonly InMemoryFlowStore _store = new();
    private readonly Recorder _recorder = new();
    private readonly IServiceProvider _services;

    public FlowEngineTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLeasingFlowEngine();
        services.AddSingleton<IFlowStore>(_store);
        services.AddSingleton<IFlowChangeNotifier>(_recorder);
        services.AddSingleton<ICreditCheckGateway>(_recorder);
        services.AddSingleton<INoticeOutbox>(_recorder);
        services.AddSingleton<ILeaseContractPublisher>(_recorder);
        services.AddSingleton<ILeaseDocuments>(_recorder);
        services.AddSingleton<IQuotePricing>(_recorder);
        _services = services.BuildServiceProvider();
    }

    private Task<T> Send<T>(IRequest<T> request) =>
        _services.CreateScope().ServiceProvider.GetRequiredService<IMediator>().Send(request);

    private static Dictionary<string, string> Application(Guid id, string assetType = "forklift", string term = "36") => new()
    {
        [FlowDataKeys.ApplicationId] = id.ToString(),
        [FlowDataKeys.ApplicantName] = "Northwind Haulage",
        [FlowDataKeys.CompanyNumber] = "RO123",
        [FlowDataKeys.AssetType] = assetType,
        [FlowDataKeys.AssetValue] = "36000",
        [FlowDataKeys.TermMonths] = term,
    };

    private async Task<FlowView> SubmitApplication(Guid id, string assetType = "forklift")
    {
        var started = await Send(new StartFlowCommand(LeaseApplicationSubmittedConsumer.Trigger, id, Application(id, assetType)));
        started.Should().Equal(id);
        return await _services.CreateScope().ServiceProvider.GetRequiredService<FlowEngine>().Get(id, default);
    }

    [Fact]
    public void Every_bpmn_task_and_action_has_a_plugin_and_every_process_a_new_flow_handler()
    {
        var catalog = _services.GetRequiredService<ProcessCatalog>();

        catalog.All.Select(p => p.ProcessId).Should().BeEquivalentTo("Application", "Assessment", "Quote", "Lease");
        var tasks = catalog.All.SelectMany(p => p.Nodes).Where(n => n.Kind is NodeKind.ServiceTask or NodeKind.UserTask).Select(n => n.Id);
        PluginRegistry.TaskIds.Should().BeEquivalentTo(tasks);
        PluginRegistry.ActionNames.Should().BeEquivalentTo(catalog.All.SelectMany(p => p.MetaData.Actions.Keys));
        PluginRegistry.NewFlowProcessIds.Should().BeEquivalentTo(catalog.All.Select(p => p.ProcessId));
    }

    [Fact]
    public async Task Submitted_application_is_validated_and_waits_for_review()
    {
        var flow = await SubmitApplication(Guid.NewGuid());

        flow.ProcessId.Should().Be("Application");
        flow.OpenTasks.Should().ContainSingle().Which.Should().Match<OpenTask>(t => t.TaskId == "ReviewApplication" && t.WorkTeam == "UnderwritingTeam");
        flow.AvailableActions.Should().Equal("Withdraw");
        flow.Status.Should().Be(FlowStatuses.InProgress);
        _recorder.ChangedFlows.Should().Equal(flow.FlowId);
    }

    [Fact]
    public async Task Unsupported_asset_is_rejected_without_review()
    {
        var flow = await SubmitApplication(Guid.NewGuid(), assetType: "yacht");

        flow.Status.Should().Be("ApplicationRejected");
        flow.OpenTasks.Should().BeEmpty();
        _store.Streams.Should().HaveCount(1);
    }

    [Fact]
    public async Task Approved_application_runs_through_assessment_quote_and_lease_to_an_active_lease()
    {
        var applicationId = Guid.NewGuid();
        await SubmitApplication(applicationId);

        var application = await Send(new ReviewApplicationCommand(applicationId, true, "fine", "underwriter-1"));
        application.Status.Should().Be("ApplicationApproved");

        var assessment = _store.Single("Assessment");
        assessment.ParentFlowId.Should().Be(applicationId);
        _recorder.CreditChecks.Should().Equal((assessment.Id, "RO123"));

        var assessed = await Send(new AssessRiskCommand(assessment.Id, "B", "risk-1"));
        assessed.Status.Should().Be("RiskAssessed");

        var quote = _store.Single("Quote");
        quote.Data[FlowDataKeys.MonthlyPayment].Should().Be("812.34");
        _recorder.PricingRequests.Should().Equal(new QuotePricingRequest("EQ-STD", "forklift", 36000m, 36));

        (await Send(new AcceptQuoteCommand(quote.Id, true, "sales-1"))).Status.Should().Be("QuoteAccepted");

        var lease = _store.Single("Lease");
        var leaseView = await _services.CreateScope().ServiceProvider.GetRequiredService<FlowEngine>().Get(lease.Id, default);
        leaseView.OpenTasks.Select(t => t.TaskId).Should().Equal("SignContract");

        var signed = await Send(new SignContractCommand(lease.Id, "Ana Pop", "ops-1"));

        signed.Status.Should().Be("LeaseActive");
        var contractNumber = lease.Data[FlowDataKeys.ContractNumber];
        _recorder.Leases.Should().ContainSingle().Which.ContractNumber.Should().Be(contractNumber);
        _recorder.Activations.Should().ContainSingle().Which.ApplicationId.Should().Be(applicationId.ToString());
        _recorder.Notices.Should().Equal((contractNumber, "Ana Pop", "lease-welcome"));
        _store.Streams.Should().HaveCount(4);
    }

    [Fact]
    public async Task Withdrawn_application_closes_and_spawns_nothing()
    {
        var id = Guid.NewGuid();
        await SubmitApplication(id);

        var flow = await Send(new ExecuteFlowActionCommand(id, "Withdraw", new Dictionary<string, string>(), "broker-1"));

        flow.Status.Should().Be(FlowStatuses.Withdrawn);
        flow.Data[FlowDataKeys.WithdrawnBy].Should().Be("broker-1");
        _store.Streams.Should().HaveCount(1);
        var act = () => Send(new ReviewApplicationCommand(id, true, null, "underwriter-1"));
        await act.Should().ThrowAsync<FlowConflictException>();
    }

    [Fact]
    public async Task Recalculate_reprices_the_quote_with_the_requested_term()
    {
        var id = Guid.NewGuid();
        await SubmitApplication(id);
        await Send(new ReviewApplicationCommand(id, true, null, "u"));
        await Send(new AssessRiskCommand(_store.Single("Assessment").Id, "A", "u"));
        var quoteId = _store.Single("Quote").Id;

        var quote = await Send(new ExecuteFlowActionCommand(quoteId, "Recalculate", new Dictionary<string, string> { [FlowDataKeys.TermMonths] = "48" }, "u"));

        _recorder.Repricings.Should().Equal((_recorder.PricingQuoteId, 48));
        quote.Data[FlowDataKeys.MonthlyPayment].Should().Be("640.00");
        quote.OpenTasks.Select(t => t.TaskId).Should().Equal("AcceptQuote");
    }

    [Fact]
    public async Task Same_application_twice_starts_one_flow()
    {
        var id = Guid.NewGuid();
        await SubmitApplication(id);

        var again = await Send(new StartFlowCommand(LeaseApplicationSubmittedConsumer.Trigger, id, Application(id)));

        again.Should().BeEmpty();
        _store.Streams.Should().HaveCount(1);
    }
}
