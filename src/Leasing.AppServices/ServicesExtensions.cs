using Lab.Core.Transform.Bpmn;
using Lab.Core.Workflow.EventFlow;
using Leasing.AppServices.EventStore;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Messaging;
using Leasing.AppServices.Projections;
using Leasing.AppServices.Search;
using Leasing.Pricing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leasing.AppServices;

public static class ServicesExtensions
{
    /// <summary>Everything the host needs except the SignalR hub: Flow Engine, MediatR, modules, stores and messaging.</summary>
    public static IServiceCollection AddLeasingAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLeasingFlowEngine();
        services.AddPricingModule(configuration);
        services.AddLeasingEventStore(configuration);
        services.AddLeasingSearch(configuration);
        services.AddLeasingMessaging(configuration);
        return services;
    }

    /// <summary>Flow Engine, its plugins and the MediatR handlers; stores and adapters are registered separately.</summary>
    public static IServiceCollection AddLeasingFlowEngine(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServicesExtensions).Assembly));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BpmnProcessLoader>();
        services.AddSingleton(sp => new ProcessCatalog(sp.GetRequiredService<BpmnProcessLoader>()));
        services.AddSingleton<EventFlowAnalyzer>();
        services.AddScoped<PluginRegistry>();
        services.AddScoped<FlowEngine>();
        services.AddScoped<FlowProjectionService>();
        PluginRegistry.Register(services);
        return services;
    }
}
