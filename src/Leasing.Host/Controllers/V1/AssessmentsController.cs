using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using Leasing.AppServices.Flows.Queries;
using Leasing.AppServices.Search;
using Leasing.Host.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leasing.Host.Controllers.V1;

public sealed record AssessRiskRequest(string RiskGrade);

[Route("v1/assessments")]
[Authorize(Policy = Policies.DefaultRead)]
public sealed class AssessmentsController(IMediator mediator) : LeasingControllerBase(mediator)
{
    private const string ProcessId = "Assessment";

    [HttpGet]
    public async Task<IReadOnlyList<FlowReadModel>> Search([FromQuery] string? status, CancellationToken cancellationToken) =>
        await Mediator.Send(new SearchFlowsQuery(ProcessId, status, null), cancellationToken);

    [HttpGet("{flowId:guid}")]
    public async Task<ActionResult<FlowView>> Get(Guid flowId, CancellationToken cancellationToken) =>
        await Mediator.Send(new GetFlowQuery(flowId, ProcessId), cancellationToken) is { } flow ? flow : NotFound();

    [HttpPost("{flowId:guid}/assess")]
    [Authorize(Policy = Policies.DefaultWrite)]
    public Task<ActionResult<FlowView>> Assess(Guid flowId, AssessRiskRequest request, CancellationToken cancellationToken) =>
        Run(() => Mediator.Send(new AssessRiskCommand(flowId, request.RiskGrade, UserId), cancellationToken));
}
