using System.Globalization;
using Lab.Core.Workflow.EventFlow.Plugins;
using Leasing.Pricing;

namespace Leasing.AppServices.Flows.Plugins.Quote;

internal static class PricingData
{
    public static void Write(FlowContext context, QuotePricingResult result)
    {
        context.SetData(FlowDataKeys.PricingQuoteId, result.PricingQuoteId.ToString());
        context.SetData(FlowDataKeys.MonthlyPayment, result.MonthlyPayment.ToString(CultureInfo.InvariantCulture));
        context.SetData(FlowDataKeys.RatePercent, result.RatePercent.ToString(CultureInfo.InvariantCulture));
        context.SetData(FlowDataKeys.ResidualPercent, result.ResidualPercent.ToString(CultureInfo.InvariantCulture));
        context.SetData(FlowDataKeys.TermMonths, result.TermMonths.ToString(CultureInfo.InvariantCulture));
    }
}
