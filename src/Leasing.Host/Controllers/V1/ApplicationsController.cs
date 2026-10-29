using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using Leasing.AppServices.Flows.Queries;
using Leasing.AppServices.Search;
using Leasing.Host.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leasing.Host.Controllers.V1;

public sealed record ReviewApplicationRequest(bool Approve, string? Comment);

[Route("v1/applications")]
[Authorize(Policy = Policies.DefaultRead)]
public sealed class ApplicationsController(IMediator mediator) : LeasingControllerBase(mediator)
{
    private const string ProcessId = "Application";

    [HttpGet]
    public async Task<IReadOnlyList<FlowReadModel>> Search([FromQuery] string? status, [FromQuery] string? q, CancellationToken cancellationToken) =>
        await Mediator.Send(new SearchFlowsQuery(ProcessId, status, q), cancellationToken);

    [HttpGet("{flowId:guid}")]
    public async Task<ActionResult<FlowView>> Get(Guid flowId, CancellationToken cancellationToken) =>
        await Mediator.Send(new GetFlowQuery(flowId, ProcessId), cancellationToken) is { } flow ? flow : NotFound();

    [HttpPost("{flowId:guid}/review")]
    [Authorize(Policy = Policies.DefaultWrite)]
    public Task<ActionResult<FlowView>> Review(Guid flowId, ReviewApplicationRequest request, CancellationToken cancellationToken) =>
        Run(() => Mediator.Send(new ReviewApplicationCommand(flowId, request.Approve, request.Comment, UserId), cancellationToken));

    [HttpPost("{flowId:guid}/actions/withdraw")]
    [Authorize(Policy = Policies.DefaultWrite)]
    public Task<ActionResult<FlowView>> Withdraw(Guid flowId, CancellationToken cancellationToken) =>
        Run(() => Mediator.Send(new ExecuteFlowActionCommand(flowId, "Withdraw", new Dictionary<string, string>(), UserId), cancellationToken));
}
