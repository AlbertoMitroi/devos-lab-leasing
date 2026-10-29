using Lab.Core.Clients.Pricing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Pricing.Client;

namespace Leasing.Pricing;

public static class PricingModuleExtensions
{
    /// <summary>
    /// Pricing module: quote calls through <c>Pricing.Client</c> (<c>Clients:Pricing</c>) and rate cards through
    /// <c>Lab.Core.Clients.Pricing</c> (<c>Clients:RateCards</c>), both with a Polly retry.
    /// </summary>
    public static IServiceCollection AddPricingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPricingClient(configuration).AddResilienceHandler("pricing-quotes", ConfigureRetry);
        services.AddRateCardClient(configuration).AddResilienceHandler("pricing-rate-cards", ConfigureRetry);
        services.AddScoped<IQuotePricing, QuotePricingService>();
        return services;
    }

    private static void ConfigureRetry(ResiliencePipelineBuilder<HttpResponseMessage> pipeline) =>
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(200),
        });
}
