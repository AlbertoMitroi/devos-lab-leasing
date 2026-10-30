# lab-leasing

Modular monolith of the equipment-leasing lab (.NET 8).

| Project | Role |
|---|---|
| `src/Leasing.Host` | ASP.NET Core host: `/v1` controllers with policies `DEFAULT_READ`, `DEFAULT_WRITE`, `ADMIN`, SignalR hub `/hubs/leasing`, Entra ID JWT (Microsoft.Identity.Web) plus a local development scheme |
| `src/Leasing.AppServices` | MediatR handlers, the Flow Engine (BPMN in `ConfigFiles/FlowEngine/Process`, `IHandle*` plugins from `Lab.Core.Workflow.EventFlow`), MassTransit bus (RabbitMQ or Azure Service Bus by `Messaging:Transport`) with the Kafka rider (Avro), Marten event store, documents and projections, Elasticsearch read model |
| `src/Leasing.Pricing` | Pricing module: `Pricing.Client` 1.1.0 and `Lab.Core.Clients.Pricing` with a Polly retry |
| `src/Leasing.AppHost` | .NET Aspire AppHost for local orchestration |
| `tools/Leasing.QuoteBackfill` | console tool pinned to `Pricing.Client` 1.0.0 on purpose |
| `tests/*` | Flow Engine end to end with in-memory ports, Pricing module against a WireMock Pricing API simulator, endpoint policy checks |

Processes: `Application` (started by `LeaseApplicationReceived` from Kafka), `Assessment` (started by status `ApplicationApproved`), `Quote` (`RiskAssessed`), `Lease` (`QuoteAccepted`).

Messaging:

| Kind | Destination (configuration key) | Direction |
|---|---|---|
| Kafka | `Kafka:Topics:Applications` = `lease-applications-v1` | consumed (from lab-intake) |
| Kafka | `Kafka:Topics:Enrichment` = `lease-application-enrichment-v1` | consumed (from lab-intake) |
| Kafka | `Kafka:Topics:Contracts` = `leasing-contracts-v1` | produced, no consumer in the lab |
| bus queue | `Messaging:Queues:CreditCheckRequests` | `CreditCheckRequested` sent |
| bus queue | `Messaging:Queues:CreditCheckResponses` | `CreditCheckResponse` consumed |
| bus queue | `Messaging:Queues:NoticeOutbox` | `PendingNoticeAddedToOutbox` sent, no consumer in the lab |
| bus queue | `Messaging:Queues:Projections` | `FlowEventsAppended` sent and consumed (ordered projections) |

## Packages

`Lab.Core.*` and `Pricing.Client` come from GitHub Packages (`nuget.config`, source `github`, with package source mapping). Credentials are never stored: set `NuGetPackageSourceCredentials_github="Username=<user>;Password=<token>"`; the Dockerfile builds it from the build secret `nuget_token`.

## Build

Docker only:

```bash
docker build --target test --secret id=nuget_token,env=NUGET_TOKEN .
```

Iterating on unpublished packages: `docker build --target test --build-arg NUGET_FEED=local --build-context localfeed=<folder with .nupkg> .`

Run the stack: `docker compose up --build` (needs `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD`, `LOCALDEV_SIGNING_KEY`, `NUGET_TOKEN`, and lab-pricing and lab-intake checked out next to this repository).

## Known warning

`NU1904` for Marten 8.38.2 (GHSA-rfx3-98h7-v3xp, LINQ string literals). The fix is in Marten 9.13, which no longer targets .NET 8; this lab stays on .NET 8 like the system it mirrors and does not use Marten LINQ queries with string literals.

## License

MIT, see [LICENSE](LICENSE).
