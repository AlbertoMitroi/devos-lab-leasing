using Leasing.AppServices.EventStore.Projections;
using Leasing.AppServices.Flows.Queries;
using Leasing.Host.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leasing.Host.Controllers.V1;

[Route("v1/admin")]
[Authorize(Policy = Policies.Admin)]
public sealed class AdminController(IMediator mediator) : LeasingControllerBase(mediator)
{
    [HttpGet("portfolio")]
    public Task<PortfolioSummary> Portfolio(CancellationToken cancellationToken) =>
        Mediator.Send(new GetPortfolioSummaryQuery(), cancellationToken);
}
