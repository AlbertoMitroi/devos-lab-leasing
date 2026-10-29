using Lab.Core.Clients.Pricing;
using Pricing.Client;

namespace Leasing.Pricing;

internal sealed class QuotePricingService(IPricingClient pricingClient, IRateCardClient rateCardClient) : IQuotePricing
{
    public async Task<QuotePricingResult> PriceAsync(QuotePricingRequest request, CancellationToken cancellationToken)
    {
        var card = await rateCardClient.GetRateCardAsync(request.ProductCode, cancellationToken)
            ?? throw new PricingUnavailableException($"No rate card for product {request.ProductCode}.");
        var term = Math.Clamp(request.TermMonths, card.MinTermMonths, card.MaxTermMonths);

        var quote = await pricingClient.CalculateQuoteAsync(
            new QuoteRequest(request.ProductCode, request.AssetType, request.AssetValue, term),
            cancellationToken);
        return ToResult(quote);
    }

    public async Task<QuotePricingResult> RepriceAsync(Guid pricingQuoteId, int termMonths, CancellationToken cancellationToken) =>
        ToResult(await pricingClient.RecalculateQuoteAsync(pricingQuoteId, termMonths, cancellationToken));

    private static QuotePricingResult ToResult(PricedQuote quote) =>
        new(quote.QuoteId, quote.MonthlyPayment, quote.RatePercent, quote.ResidualPercent, quote.TermMonths);
}

public sealed class PricingUnavailableException(string message) : Exception(message);
