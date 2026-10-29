using System.Globalization;
using Lab.Core.Workflow.EventFlow.Plugins;

namespace Leasing.AppServices.Flows.Plugins.Application;

public sealed class ValidateApplicationHandler : IHandleServiceTask
{
    public static readonly string[] SupportedAssetTypes = ["forklift", "excavator"];

    public Task<string?> Handle(FlowContext context, CancellationToken cancellationToken)
    {
        var valid = context.Data.TryGetValue(FlowDataKeys.AssetType, out var assetType) && SupportedAssetTypes.Contains(assetType)
            && decimal.TryParse(context.Data.GetValueOrDefault(FlowDataKeys.AssetValue), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0
            && int.TryParse(context.Data.GetValueOrDefault(FlowDataKeys.TermMonths), CultureInfo.InvariantCulture, out var term) && term is >= 6 and <= 60;
        return Task.FromResult<string?>(valid ? "Valid" : "Invalid");
    }
}
