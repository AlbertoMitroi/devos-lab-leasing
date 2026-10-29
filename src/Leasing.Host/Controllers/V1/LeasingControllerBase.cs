using System.Security.Claims;
using Leasing.AppServices.Flows;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leasing.Host.Controllers.V1;

[ApiController]
[Produces("application/json")]
public abstract class LeasingControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator => mediator;

    protected string? UserId => User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected async Task<ActionResult<FlowView>> Run(Func<Task<FlowView>> command)
    {
        try
        {
            return await command();
        }
        catch (FlowNotFoundException)
        {
            return NotFound();
        }
        catch (FlowConflictException e)
        {
            return Conflict(new ProblemDetails { Title = "Flow conflict", Detail = e.Message });
        }
    }
}
