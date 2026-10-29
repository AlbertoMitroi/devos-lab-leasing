namespace Leasing.AppServices.Search;

/// <summary>Document of the <c>Elasticsearch:Indexes:Flows</c> index, read by the list screens of the web app.</summary>
public sealed record FlowReadModel(
    Guid FlowId,
    string ProcessId,
    string Status,
    Guid? ParentFlowId,
    IReadOnlyList<string> OpenTasks,
    IReadOnlyList<string> AvailableActions,
    string? ApplicantName,
    string? AssetType,
    decimal? MonthlyPayment,
    string? ContractNumber,
    int Version,
    DateTimeOffset UpdatedAt);

public interface IFlowSearch
{
    Task Index(FlowReadModel document, CancellationToken cancellationToken);

    Task<IReadOnlyList<FlowReadModel>> Search(string processId, string? status, string? text, CancellationToken cancellationToken);
}
