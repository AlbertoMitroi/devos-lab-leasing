using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Lab.Intake.Contracts;
using Lab.Leasing.Contracts;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Messaging.Consumers;
using Leasing.AppServices.Messaging.Kafka;
using Leasing.AppServices.Messaging.Simulators;
using Leasing.AppServices.Ports;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leasing.AppServices.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// One MassTransit bus: RabbitMQ or Azure Service Bus by <c>Messaging:Transport</c>, with the Kafka rider attached
    /// (Avro, topics from <c>Kafka:Topics</c>) unless <c>Kafka:Enabled</c> is false.
    /// </summary>
    public static IServiceCollection AddLeasingMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var messaging = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
        var kafka = configuration.GetSection(LeasingKafkaOptions.SectionName).Get<LeasingKafkaOptions>() ?? new LeasingKafkaOptions();
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        services.AddScoped<ICreditCheckGateway, BusCreditCheckGateway>();
        services.AddScoped<INoticeOutbox, BusNoticeOutbox>();
        services.AddScoped<IFlowChangeNotifier, BusFlowChangeNotifier>();

        if (kafka.Enabled)
        {
            services.AddSingleton<ISchemaRegistryClient>(_ => new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = kafka.SchemaRegistryUrl }));
            services.AddScoped<ILeaseContractPublisher, KafkaLeaseContractPublisher>();
        }
        else
        {
            services.AddSingleton<ILeaseContractPublisher, DisabledLeaseContractPublisher>();
        }

        services.AddMassTransit(x =>
        {
            x.AddConsumer<CreditCheckResponseConsumer>();
            x.AddConsumer<FlowEventsAppendedConsumer>();
            if (messaging.CreditBureauSimulator)
            {
                x.AddConsumer<CreditBureauSimulatorConsumer>();
            }

            if (kafka.Enabled)
            {
                x.AddRider(rider => ConfigureKafka(rider, kafka));
            }

            if (messaging.Transport == MessagingOptions.AzureServiceBus)
            {
                x.UsingAzureServiceBus((context, bus) =>
                {
                    bus.Host(messaging.AzureServiceBusHost.ConnectionString);
                    bus.UseDelayedRedelivery(r => r.Intervals(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)));
                    ConfigureEndpoints(context, bus, messaging);
                });
            }
            else
            {
                x.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(messaging.RabbitMqHost.Host, messaging.RabbitMqHost.VirtualHost, h =>
                    {
                        h.Username(messaging.RabbitMqHost.Username);
                        h.Password(messaging.RabbitMqHost.Password);
                    });
                    ConfigureEndpoints(context, bus, messaging);
                });
            }
        });

        return services;
    }

    private static void ConfigureEndpoints(IBusRegistrationContext context, IReceiveConfigurator bus, MessagingOptions messaging)
    {
        bus.ReceiveEndpoint(messaging.Queues.CreditCheckResponses, e =>
        {
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(1)));
            e.ConfigureConsumer<CreditCheckResponseConsumer>(context);
        });

        bus.ReceiveEndpoint(messaging.Queues.Projections, e =>
        {
            e.ConcurrentMessageLimit = 1;
            e.ConfigureConsumer<FlowEventsAppendedConsumer>(context);
        });

        if (messaging.CreditBureauSimulator)
        {
            bus.ReceiveEndpoint(messaging.Queues.CreditCheckRequests, e => e.ConfigureConsumer<CreditBureauSimulatorConsumer>(context));
        }
    }

    private static void ConfigureKafka(IRiderRegistrationConfigurator rider, LeasingKafkaOptions kafka)
    {
        rider.AddConsumer<LeaseApplicationSubmittedConsumer>();
        rider.AddConsumer<LeaseApplicationEnrichedConsumer>();

        rider.AddProducer<string, LeaseContractActivated>(kafka.Topics.Contracts, (context, producer) =>
            producer.SetValueSerializer(new AvroSerializer<LeaseContractActivated>(context.GetRequiredService<ISchemaRegistryClient>())));

        rider.UsingKafka((context, k) =>
        {
            k.Host(kafka.BootstrapServers);

            k.TopicEndpoint<string, LeaseApplicationSubmitted>(kafka.Topics.Applications, kafka.ConsumerGroup, e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.SetValueDeserializer(new AvroDeserializer<LeaseApplicationSubmitted>(context.GetRequiredService<ISchemaRegistryClient>()).AsSyncOverAsync());
                e.ConfigureConsumer<LeaseApplicationSubmittedConsumer>(context);
            });

            k.TopicEndpoint<string, LeaseApplicationEnriched>(kafka.Topics.Enrichment, kafka.ConsumerGroup, e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.SetValueDeserializer(new AvroDeserializer<LeaseApplicationEnriched>(context.GetRequiredService<ISchemaRegistryClient>()).AsSyncOverAsync());
                e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(2)));
                e.ConfigureConsumer<LeaseApplicationEnrichedConsumer>(context);
            });
        });
    }
}
