using Lab.Core.Workflow.EventFlow.Plugins;
using MediatR;

namespace Leasing.AppServices.Flows.Commands;

public sealed record ReviewApplicationCommand(Guid FlowId, bool Approve, string? Comment, string? UserId) : IRequest<FlowView>, IUserTaskCommand;

public sealed record AssessRiskCommand(Guid FlowId, string RiskGrade, string? UserId) : IRequest<FlowView>, IUserTaskCommand;

public sealed record AcceptQuoteCommand(Guid FlowId, bool Accept, string? UserId) : IRequest<FlowView>, IUserTaskCommand;

public sealed record SignContractCommand(Guid FlowId, string SignedBy, string? UserId) : IRequest<FlowView>, IUserTaskCommand;

public sealed class UserTaskCommandHandlers(FlowEngine engine) :
    IRequestHandler<ReviewApplicationCommand, FlowView>,
    IRequestHandler<AssessRiskCommand, FlowView>,
    IRequestHandler<AcceptQuoteCommand, FlowView>,
    IRequestHandler<SignContractCommand, FlowView>
{
    public Task<FlowView> Handle(ReviewApplicationCommand request, CancellationToken cancellationToken) =>
        engine.CompleteUserTask("ReviewApplication", request, request.UserId, cancellationToken);

    public Task<FlowView> Handle(AssessRiskCommand request, CancellationToken cancellationToken) =>
        engine.CompleteUserTask("AssessRisk", request, request.UserId, cancellationToken);

    public Task<FlowView> Handle(AcceptQuoteCommand request, CancellationToken cancellationToken) =>
        engine.CompleteUserTask("AcceptQuote", request, request.UserId, cancellationToken);

    public Task<FlowView> Handle(SignContractCommand request, CancellationToken cancellationToken) =>
        engine.CompleteUserTask("SignContract", request, request.UserId, cancellationToken);
}
