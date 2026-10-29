using System.Globalization;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Flows.Commands;
using Leasing.AppServices.Flows.Queries;
using Leasing.AppServices.Search;
using Leasing.Host.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leasing.Host.Controllers.V1;

public sealed record AcceptQuoteRequest(bool Accept);

public sealed record RecalculateQuoteRequest(int TermMonths);

[Route("v1/quotes")]
[Authorize(Policy = Policies.DefaultRead)]
public sealed class QuotesController(IMediator mediator) : LeasingControllerBase(mediator)
{
    private const string ProcessId = "Quote";

    [HttpGet]
    public async Task<IReadOnlyList<FlowReadModel>> Search([FromQuery] string? status, CancellationToken cancellationToken) =>
        await Mediator.Send(new SearchFlowsQuery(ProcessId, status, null), cancellationToken);

    [HttpGet("{flowId:guid}")]
    public async Task<ActionResult<FlowView>> Get(Guid flowId, CancellationToken cancellationToken) =>
        await Mediator.Send(new GetFlowQuery(flowId, ProcessId), cancellationToken) is { } flow ? flow : NotFound();

    [HttpPost("{flowId:guid}/accept")]
    [Authorize(Policy = Policies.DefaultWrite)]
    public Task<ActionResult<FlowView>> Accept(Guid flowId, AcceptQuoteRequest request, CancellationToken cancellationToken) =>
        Run(() => Mediator.Send(new AcceptQuoteCommand(flowId, request.Accept, UserId), cancellationToken));

    [HttpPost("{flowId:guid}/recalculate")]
    [Authorize(Policy = Policies.DefaultWrite)]
    public Task<ActionResult<FlowView>> Recalculate(Guid flowId, RecalculateQuoteRequest request, CancellationToken cancellationToken) =>
        Run(() => Mediator.Send(
            new ExecuteFlowActionCommand(
                flowId,
                "Recalculate",
                new Dictionary<string, string> { [FlowDataKeys.TermMonths] = request.TermMonths.ToString(CultureInfo.InvariantCulture) },
                UserId),
            cancellationToken));
}
