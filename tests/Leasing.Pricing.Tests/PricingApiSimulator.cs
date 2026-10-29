using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Leasing.Pricing.Tests;

/// <summary>WireMock stand-in for the Pricing API (lab-pricing), with the routes the two client packages call.</summary>
public sealed class PricingApiSimulator : IDisposable
{
    public static readonly Guid QuoteId = Guid.Parse("6f1c2a5e-0000-4000-8000-000000000001");

    public WireMockServer Server { get; } = WireMockServer.Start();

    public string Url => Server.Url! + "/";

    public PricingApiSimulator()
    {
        Server.Given(Request.Create().WithPath("/api/rate-cards/EQ-STD").UsingGet())
            .RespondWith(Response.Create().WithBodyAsJson(new { productCode = "EQ-STD", baseRatePercent = 5.9, minTermMonths = 12, maxTermMonths = 60 }));

        // The first quote request fails, the retry of the resilience pipeline gets the quote.
        Server.Given(Request.Create().WithPath("/api/quotes").UsingPost())
            .InScenario("quote").WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        Server.Given(Request.Create().WithPath("/api/quotes").UsingPost())
            .InScenario("quote").WhenStateIs("recovered")
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(Quote(60, 690.12)));

        Server.Given(Request.Create().WithPath($"/api/quotes/{QuoteId}/recalculate").UsingPost())
            .RespondWith(Response.Create().WithBodyAsJson(Quote(48, 701.50)));
    }

    private static object Quote(int termMonths, double payment) => new
    {
        quoteId = QuoteId,
        productCode = "EQ-STD",
        assetType = "forklift",
        assetValue = 36000,
        termMonths,
        ratePercent = 5.9,
        residualPercent = 18,
        monthlyPayment = payment,
        createdAt = "2026-10-29T10:00:00+00:00",
    };

    public void Dispose() => Server.Stop();
}
