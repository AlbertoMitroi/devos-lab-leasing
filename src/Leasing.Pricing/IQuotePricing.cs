namespace Leasing.Pricing;

/// <summary>Prices leasing quotes; the only entry point of the Pricing module for the rest of the backend.</summary>
public interface IQuotePricing
{
    Task<QuotePricingResult> PriceAsync(QuotePricingRequest request, CancellationToken cancellationToken);

    Task<QuotePricingResult> RepriceAsync(Guid pricingQuoteId, int termMonths, CancellationToken cancellationToken);
}
