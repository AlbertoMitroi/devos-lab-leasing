namespace Leasing.AppServices.Messaging;

/// <summary>Sent to the credit bureau adapter (queue <c>Messaging:Queues:CreditCheckRequests</c>).</summary>
public sealed record CreditCheckRequested(Guid FlowId, string CompanyNumber);

/// <summary>Answer of the credit bureau adapter (queue <c>Messaging:Queues:CreditCheckResponses</c>).</summary>
public sealed record CreditCheckResponse(Guid FlowId, int CreditScore);

/// <summary>Sent to the notice outbox service (queue <c>Messaging:Queues:NoticeOutbox</c>).</summary>
public sealed record PendingNoticeAddedToOutbox(Guid FlowId, string ContractNumber, string Recipient, string Template);

/// <summary>Ordered projection message (queue <c>Messaging:Queues:Projections</c>).</summary>
public sealed record FlowEventsAppended(Guid FlowId);
