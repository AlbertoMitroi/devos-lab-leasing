using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.AppServices.Flows.Commands;

namespace Leasing.AppServices.Flows.Plugins.Quote;

public sealed class AcceptQuoteHandler : IHandleUserTaskCommand<AcceptQuoteCommand>
{
    public Task<string?> Handle(AcceptQuoteCommand command, FlowContext context, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(command.Accept ? "Accepted" : "Declined");
}
