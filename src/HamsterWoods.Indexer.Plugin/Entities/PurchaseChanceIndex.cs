using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace HamsterWoods.Indexer.Plugin.Entities;

public class PurchaseChanceIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string Chainid { get; set; }
    public int Chance { get; set; }
    public int Cost { get; set; }
    public int WeekNum { get; set; }
    public TransactionInfoIndex? TransactionInfo { get; set; }
}