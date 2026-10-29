using Leasing.AppServices.EventStore;
using Leasing.AppServices.EventStore.Projections;
using Marten;
using MediatR;

namespace Leasing.AppServices.Flows.Queries;

public sealed record GetPortfolioSummaryQuery : IRequest<PortfolioSummary>;

public sealed record GetLeaseDocumentQuery(Guid LeaseFlowId) : IRequest<LeaseDocument?>;

public sealed class PortfolioQueryHandlers(IQuerySession session) :
    IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummary>,
    IRequestHandler<GetLeaseDocumentQuery, LeaseDocument?>
{
    public async Task<PortfolioSummary> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken) =>
        await session.LoadAsync<PortfolioSummary>(PortfolioSummary.DocumentId, cancellationToken) ?? new PortfolioSummary();

    public Task<LeaseDocument?> Handle(GetLeaseDocumentQuery request, CancellationToken cancellationToken) =>
        session.LoadAsync<LeaseDocument>(request.LeaseFlowId, cancellationToken);
}
