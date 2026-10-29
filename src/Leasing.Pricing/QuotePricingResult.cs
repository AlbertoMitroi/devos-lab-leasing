namespace Leasing.Pricing;

public sealed record QuotePricingRequest(string ProductCode, string AssetType, decimal AssetValue, int TermMonths);

public sealed record QuotePricingResult(Guid PricingQuoteId, decimal MonthlyPayment, decimal RatePercent, decimal ResidualPercent, int TermMonths);
