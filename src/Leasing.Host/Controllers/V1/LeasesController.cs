using Leasing.AppServices.EventStore;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using Leasing.AppServices.Flows.Queries;
using Leasing.AppServices.Search;
using Leasing.Host.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leasing.Host.Controllers.V1;

public sealed record SignContractRequest(string SignedBy);

[Route("v1/leases")]
[Authorize(Policy = Policies.DefaultRead)]
public sealed class LeasesController(IMediator mediator) : LeasingControllerBase(mediator)
{
    private const string ProcessId = "Lease";

    [HttpGet]
    public async Task<IReadOnlyList<FlowReadModel>> Search([FromQuery] string? status, [FromQuery] string? q, CancellationToken cancellationToken) =>
        await Mediator.Send(new SearchFlowsQuery(ProcessId, status, q), cancellationToken);

    [HttpGet("{flowId:guid}")]
    public async Task<ActionResult<FlowView>> Get(Guid flowId, CancellationToken cancellationToken) =>
        await Mediator.Send(new GetFlowQuery(flowId, ProcessId), cancellationToken) is { } flow ? flow : NotFound();

    [HttpGet("{flowId:guid}/document")]
    public async Task<ActionResult<LeaseDocument>> GetDocument(Guid flowId, CancellationToken cancellationToken) =>
        await Mediator.Send(new GetLeaseDocumentQuery(flowId), cancellationToken) is { } lease ? lease : NotFound();

    [HttpPost("{flowId:guid}/sign")]
    [Authorize(Policy = Policies.DefaultWrite)]
    public Task<ActionResult<FlowView>> Sign(Guid flowId, SignContractRequest request, CancellationToken cancellationToken) =>
        Run(() => Mediator.Send(new SignContractCommand(flowId, request.SignedBy, UserId), cancellationToken));
}
