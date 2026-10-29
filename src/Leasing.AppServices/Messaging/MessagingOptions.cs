namespace Leasing.AppServices.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";
    public const string RabbitMq = "RabbitMq";
    public const string AzureServiceBus = "AzureServiceBus";

    /// <summary><c>RabbitMq</c> (Local, Docker) or <c>AzureServiceBus</c> (cloud environments).</summary>
    public string Transport { get; set; } = RabbitMq;

    public RabbitMqOptions RabbitMqHost { get; set; } = new();

    public AzureServiceBusOptions AzureServiceBusHost { get; set; } = new();

    public QueueNames Queues { get; set; } = new();

    public bool CreditBureauSimulator { get; set; }
}

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "";
}

public sealed class AzureServiceBusOptions
{
    public string ConnectionString { get; set; } = "";
}

public sealed class QueueNames
{
    public string CreditCheckRequests { get; set; } = "credit-check-requests";

    public string CreditCheckResponses { get; set; } = "leasing-credit-check-responses";

    public string NoticeOutbox { get; set; } = "notice-outbox";

    public string Projections { get; set; } = "leasing-ordered-projection-messages";
}

public sealed class LeasingKafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; } = true;

    public string BootstrapServers { get; set; } = "";

    public string SchemaRegistryUrl { get; set; } = "";

    public string ConsumerGroup { get; set; } = "leasing";

    public LeasingTopics Topics { get; set; } = new();
}

public sealed class LeasingTopics
{
    public string Applications { get; set; } = "lease-applications-v1";

    public string Enrichment { get; set; } = "lease-application-enrichment-v1";

    public string Contracts { get; set; } = "leasing-contracts-v1";
}
