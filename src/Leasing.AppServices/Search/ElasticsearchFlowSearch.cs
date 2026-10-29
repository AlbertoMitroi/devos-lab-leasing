using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leasing.AppServices.Search;

public sealed class ElasticsearchFlowSearch(ElasticsearchClient client, IOptions<ElasticsearchOptions> options) : IFlowSearch
{
    private string IndexName => options.Value.Indexes.Flows;

    public async Task Index(FlowReadModel document, CancellationToken cancellationToken)
    {
        var response = await client.IndexAsync(document, IndexName, document.FlowId.ToString(), cancellationToken);
        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Indexing flow {document.FlowId} failed: {response.DebugInformation}");
        }
    }

    public async Task<IReadOnlyList<FlowReadModel>> Search(string processId, string? status, string? text, CancellationToken cancellationToken)
    {
        var filters = new List<Query> { new TermQuery { Field = "processId.keyword"!, Value = processId } };
        if (!string.IsNullOrWhiteSpace(status))
        {
            filters.Add(new TermQuery { Field = "status.keyword"!, Value = status });
        }

        var must = new List<Query>();
        if (!string.IsNullOrWhiteSpace(text))
        {
            must.Add(new MultiMatchQuery { Query = text, Fields = new[] { "applicantName", "contractNumber", "assetType" } });
        }

        var request = new SearchRequest<FlowReadModel>(IndexName)
        {
            Size = 50,
            Query = new BoolQuery { Filter = filters, Must = must },
        };
        var response = await client.SearchAsync<FlowReadModel>(request, cancellationToken);
        return response.IsValidResponse ? response.Documents.ToList() : [];
    }
}

public static class SearchExtensions
{
    public static IServiceCollection AddLeasingSearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            return new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(options.Url)).DefaultIndex(options.Indexes.Flows));
        });
        services.AddScoped<IFlowSearch, ElasticsearchFlowSearch>();
        return services;
    }
}
