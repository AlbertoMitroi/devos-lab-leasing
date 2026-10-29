using Leasing.AppServices.Search;
using MediatR;

namespace Leasing.AppServices.Flows.Queries;

public sealed record GetFlowQuery(Guid FlowId, string ProcessId) : IRequest<FlowView?>;

public sealed record SearchFlowsQuery(string ProcessId, string? Status, string? Text) : IRequest<IReadOnlyList<FlowReadModel>>;

public sealed class GetFlowQueryHandler(FlowEngine engine) : IRequestHandler<GetFlowQuery, FlowView?>
{
    public async Task<FlowView?> Handle(GetFlowQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var flow = await engine.Get(request.FlowId, cancellationToken);
            return flow.ProcessId == request.ProcessId ? flow : null;
        }
        catch (FlowNotFoundException)
        {
            return null;
        }
    }
}

public sealed class SearchFlowsQueryHandler(IFlowSearch search) : IRequestHandler<SearchFlowsQuery, IReadOnlyList<FlowReadModel>>
{
    public Task<IReadOnlyList<FlowReadModel>> Handle(SearchFlowsQuery request, CancellationToken cancellationToken) =>
        search.Search(request.ProcessId, request.Status, request.Text, cancellationToken);
}
