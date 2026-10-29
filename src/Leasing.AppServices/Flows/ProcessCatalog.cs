using Lab.Core.Transform.Bpmn;

namespace Leasing.AppServices.Flows;

/// <summary>The BPMN processes of the backend, loaded once from <c>ConfigFiles/FlowEngine/Process</c>.</summary>
public sealed class ProcessCatalog
{
    public const string ProcessDirectory = "ConfigFiles/FlowEngine/Process";

    private readonly Dictionary<string, ProcessDefinition> _processes;

    public ProcessCatalog(BpmnProcessLoader loader)
        : this(loader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, ProcessDirectory)))
    {
    }

    public ProcessCatalog(IEnumerable<ProcessDefinition> processes) =>
        _processes = processes.ToDictionary(p => p.ProcessId, StringComparer.Ordinal);

    public IReadOnlyCollection<ProcessDefinition> All => _processes.Values;

    public ProcessDefinition Get(string processId) =>
        _processes.TryGetValue(processId, out var process)
            ? process
            : throw new InvalidOperationException($"Unknown process '{processId}'.");

    /// <summary>Processes whose metadata lists <paramref name="trigger"/> (an event, fact or status) in <c>StartedBy</c>.</summary>
    public IEnumerable<ProcessDefinition> StartedBy(string trigger) =>
        _processes.Values.Where(p => p.MetaData.StartedBy.Contains(trigger, StringComparer.Ordinal)).OrderBy(p => p.ProcessId, StringComparer.Ordinal);
}
