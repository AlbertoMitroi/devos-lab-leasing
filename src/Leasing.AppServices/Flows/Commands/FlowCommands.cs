using MediatR;

namespace Leasing.AppServices.Flows.Commands;

/// <summary>Starts the flows of every process started by <paramref name="Trigger"/>.</summary>
public sealed record StartFlowCommand(string Trigger, Guid? FlowId, IReadOnlyDictionary<string, string> Data) : IRequest<IReadOnlyList<Guid>>;

public sealed record UpdateFlowDataCommand(Guid FlowId, IReadOnlyDictionary<string, string> Data) : IRequest<FlowView>;

public sealed record ExecuteFlowActionCommand(Guid FlowId, string ActionName, IReadOnlyDictionary<string, string> Parameters, string? UserId) : IRequest<FlowView>;

public sealed class StartFlowCommandHandler(FlowEngine engine) : IRequestHandler<StartFlowCommand, IReadOnlyList<Guid>>
{
    public Task<IReadOnlyList<Guid>> Handle(StartFlowCommand request, CancellationToken cancellationToken) =>
        engine.StartFlows(request.Trigger, request.FlowId, null, request.Data, cancellationToken);
}

public sealed class UpdateFlowDataCommandHandler(FlowEngine engine) : IRequestHandler<UpdateFlowDataCommand, FlowView>
{
    public Task<FlowView> Handle(UpdateFlowDataCommand request, CancellationToken cancellationToken) =>
        engine.UpdateData(request.FlowId, request.Data, cancellationToken);
}

public sealed class ExecuteFlowActionCommandHandler(FlowEngine engine) : IRequestHandler<ExecuteFlowActionCommand, FlowView>
{
    public Task<FlowView> Handle(ExecuteFlowActionCommand request, CancellationToken cancellationToken) =>
        engine.ExecuteAction(request.FlowId, request.ActionName, request.Parameters, request.UserId, cancellationToken);
}
