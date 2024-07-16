using AElfIndexer.Client.GraphQL;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class HamsterWoodsIndexerPluginSchema : AElfIndexerClientSchema<Query>
{
    public HamsterWoodsIndexerPluginSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}