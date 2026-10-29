// Reads quotes from the Pricing API and prints their monthly payment, one line per quote id given on the command line.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pricing.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPricingClient(builder.Configuration);
using var host = builder.Build();

var client = host.Services.GetRequiredService<IPricingClient>();
foreach (var arg in args.Where(a => Guid.TryParse(a, out _)))
{
    var quote = await client.GetQuoteAsync(Guid.Parse(arg));
    Console.WriteLine(quote is null ? $"{arg}: not found" : $"{arg}: {quote.MonthlyPayment} x {quote.TermMonths}");
}
