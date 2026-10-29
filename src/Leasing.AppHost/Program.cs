// Local orchestration: infrastructure containers, the sibling Pricing API built from ../lab-pricing, and the leasing host.
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();
var leasingDb = postgres.AddDatabase("LeasingDb", "leasing");
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();
var kafka = builder.AddKafka("kafka").WithKafkaUI();
var elasticsearch = builder.AddElasticsearch("elasticsearch");

var pricingApi = builder.AddDockerfile("pricing-api", "../../../lab-pricing", stage: "runtime")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Docker");

builder.AddProject<Projects.Leasing_Host>("leasing-host")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Local")
    .WithReference(leasingDb)
    .WithReference(rabbitmq)
    .WithReference(kafka)
    .WithReference(elasticsearch)
    .WithEnvironment("Clients__Pricing__BaseAddress", pricingApi.GetEndpoint("http"))
    .WithEnvironment("Clients__RateCards__BaseAddress", pricingApi.GetEndpoint("http"))
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

builder.Build().Run();
