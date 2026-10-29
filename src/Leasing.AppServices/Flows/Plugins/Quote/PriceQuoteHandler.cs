using System.Globalization;
using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.Pricing;

namespace Leasing.AppServices.Flows.Plugins.Quote;

public sealed class PriceQuoteHandler(IQuotePricing pricing) : IHandleServiceTask
{
    public async Task<string?> Handle(FlowContext context, CancellationToken cancellationToken)
    {
        var result = await pricing.PriceAsync(
            new QuotePricingRequest(
                context.Data[FlowDataKeys.ProductCode],
                context.Data[FlowDataKeys.AssetType],
                decimal.Parse(context.Data[FlowDataKeys.AssetValue], CultureInfo.InvariantCulture),
                int.Parse(context.Data[FlowDataKeys.TermMonths], CultureInfo.InvariantCulture)),
            cancellationToken);
        PricingData.Write(context, result);
        return null;
    }
}
