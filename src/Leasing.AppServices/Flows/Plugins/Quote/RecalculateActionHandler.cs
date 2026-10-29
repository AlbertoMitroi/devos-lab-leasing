using System.Globalization;
using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.Pricing;

namespace Leasing.AppServices.Flows.Plugins.Quote;

/// <summary>Reprices the quote with the term in the <c>termMonths</c> action parameter.</summary>
public sealed class RecalculateActionHandler(IQuotePricing pricing) : IHandleAction
{
    public async Task Handle(FlowContext context, CancellationToken cancellationToken)
    {
        var result = await pricing.RepriceAsync(
            Guid.Parse(context.Data[FlowDataKeys.PricingQuoteId]),
            int.Parse(context.Data[FlowDataKeys.TermMonths], CultureInfo.InvariantCulture),
            cancellationToken);
        PricingData.Write(context, result);
    }
}
