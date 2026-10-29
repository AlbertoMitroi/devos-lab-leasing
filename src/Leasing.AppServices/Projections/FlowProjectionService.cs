using System.Globalization;
using Lab.Core.MessageHubs;
using Lab.Core.Transform.Bpmn;
using Lab.Core.Workflow.EventFlow;
using Leasing.AppServices.Flows;
using Leasing.AppServices.Search;
using Microsoft.Extensions.Configuration;

namespace Leasing.AppServices.Projections;

/// <summary>
/// Builds the read model of a flow after its events were appended, indexes it in Elasticsearch
/// and pushes it to the web app over SignalR.
/// </summary>
public sealed class FlowProjectionService(
    IFlowStore store,
    ProcessCatalog catalog,
    EventFlowAnalyzer analyzer,
    IFlowSearch search,
    IMessageHubUtility hub,
    IConfiguration configuration,
    TimeProvider clock)
{
    public const string FlowUpdatedMethod = "flowUpdated";
    public const string TaskAssignedMethod = "taskAssigned";

    public async Task Project(Guid flowId, CancellationToken cancellationToken)
    {
        var flow = await store.Load(flowId, cancellationToken);
        if (flow is null)
        {
            return;
        }

        var analysis = analyzer.Analyze(catalog.Get(flow.ProcessId), flow);
        var document = new FlowReadModel(
            flow.Id,
            flow.ProcessId,
            analysis.Status,
            flow.ParentFlowId,
            analysis.OpenTasks.Select(t => t.TaskId).ToList(),
            analysis.AvailableActions,
            flow.Data.GetValueOrDefault(FlowDataKeys.ApplicantName),
            flow.Data.GetValueOrDefault(FlowDataKeys.AssetType),
            decimal.TryParse(flow.Data.GetValueOrDefault(FlowDataKeys.MonthlyPayment), NumberStyles.Number, CultureInfo.InvariantCulture, out var payment) ? payment : null,
            flow.Data.GetValueOrDefault(FlowDataKeys.ContractNumber),
            flow.Version,
            clock.GetUtcNow());

        await search.Index(document, cancellationToken);
        await hub.SendToAll(FlowUpdatedMethod, document, cancellationToken);

        foreach (var task in analysis.OpenTasks.Where(t => t.Kind == NodeKind.UserTask && t.WorkTeam is not null))
        {
            var members = configuration.GetSection($"WorkTeams:{task.WorkTeam}:Members").Get<string[]>() ?? [];
            if (members.Length > 0)
            {
                await hub.SendToSpecifiedUsers(members, TaskAssignedMethod, new { flow.Id, flow.ProcessId, task.TaskId, task.Name }, cancellationToken);
            }
        }
    }
}
