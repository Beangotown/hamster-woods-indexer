using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace HamsterWoods.Indexer.Plugin.Entities;

public class UnLockAcornsIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string FromAddress { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string Chainid { get; set; }

    public long Amount { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; } = 8;

    public int WeekNum { get; set; }
    public DateTime BlockTime { get; set; }
    public TransactionInfoIndex TransactionInfo { get; set; }
}