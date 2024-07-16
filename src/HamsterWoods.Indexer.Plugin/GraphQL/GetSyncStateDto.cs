using AElfIndexer;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetSyncStateDto
{
    public string ChainId { get; set; }
    public BlockFilterType FilterType { get; set; }
}