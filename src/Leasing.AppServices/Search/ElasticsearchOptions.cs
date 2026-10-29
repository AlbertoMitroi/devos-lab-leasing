namespace Leasing.AppServices.Search;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";

    public ElasticsearchIndexes Indexes { get; set; } = new();
}

public sealed class ElasticsearchIndexes
{
    public string Flows { get; set; } = "leasing-flows";
}
