using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leasing.Pricing.Tests;

public class QuotePricingTests : IDisposable
{
    private readonly PricingApiSimulator _simulator = new();
    private readonly IQuotePricing _pricing;

    public QuotePricingTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Clients:Pricing:BaseAddress"] = _simulator.Url,
                ["Clients:RateCards:BaseAddress"] = _simulator.Url,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPricingModule(configuration);
        _pricing = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IQuotePricing>();
    }

    [Fact]
    public async Task Prices_with_the_term_clamped_to_the_rate_card_and_retries_a_failed_call()
    {
        var result = await _pricing.PriceAsync(new QuotePricingRequest("EQ-STD", "forklift", 36_000m, 72), default);

        result.Should().Be(new QuotePricingResult(PricingApiSimulator.QuoteId, 690.12m, 5.9m, 18m, 60));
        var quoteCalls = _simulator.Server.LogEntries.Where(e => e.RequestMessage.Path == "/api/quotes").ToList();
        quoteCalls.Should().HaveCount(2);
        quoteCalls[^1].RequestMessage.Body.Should().Contain("\"termMonths\":60");
    }

    [Fact]
    public async Task Reprices_through_the_1_1_client_method()
    {
        var result = await _pricing.RepriceAsync(PricingApiSimulator.QuoteId, 48, default);

        result.TermMonths.Should().Be(48);
        result.MonthlyPayment.Should().Be(701.50m);
    }

    [Fact]
    public async Task Unknown_product_has_no_rate_card()
    {
        var act = () => _pricing.PriceAsync(new QuotePricingRequest("NONE", "forklift", 1m, 12), default);

        await act.Should().ThrowAsync<Exception>();
    }

    public void Dispose() => _simulator.Dispose();
}
