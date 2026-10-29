using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Lab.Core.Workflow.EventFlow;
using Leasing.AppServices.EventStore.Projections;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Ports;
using Marten;
using Marten.Events.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leasing.AppServices.EventStore;

public static class EventStoreExtensions
{
    public const string ConnectionStringName = "LeasingDb";
    public const string SchemaName = "leasing";

    /// <summary>Marten on PostgreSQL: flow event streams, the inline flow snapshot and timeline, the async portfolio summary and lease documents.</summary>
    public static IServiceCollection AddLeasingEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMarten(options =>
            {
                options.Connection(configuration.GetConnectionString(ConnectionStringName) ?? "");
                options.DatabaseSchemaName = SchemaName;
                options.Events.AddEventTypes([
                    typeof(FlowStarted), typeof(TaskCompleted), typeof(ActionExecuted),
                    typeof(FlowDataUpdated), typeof(FlowStatusChanged), typeof(FlowFactRaised),
                ]);
                options.Projections.Snapshot<FlowAggregate>(SnapshotLifecycle.Inline);
                options.Projections.Add<FlowTimelineProjection>(ProjectionLifecycle.Inline);
                options.Projections.Add<PortfolioSummaryProjection>(ProjectionLifecycle.Async);
                options.Schema.For<LeaseDocument>().Index(x => x.ContractNumber);
            })
            .UseLightweightSessions()
            .AddAsyncDaemon(DaemonMode.Solo);

        services.AddScoped<MartenFlowStore>();
        services.AddScoped<IFlowStore>(sp => sp.GetRequiredService<MartenFlowStore>());
        services.AddScoped<ILeaseDocuments>(sp => sp.GetRequiredService<MartenFlowStore>());
        return services;
    }
}
