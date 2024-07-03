using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Contracts.HamsterWoods;
using Nest;

namespace HamsterWoods.Indexer.Plugin.Entities;

public class GameIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string Chainid { get; set; }
    public long PlayBlockHeight { get; set; }
    public bool IsComplete { get; set; }
    public GridType GridType { get; set; }
    public int GridNum { get; set; }
    public int Score { get; set; }
    public int WeekNum { get; set; }
    public bool IsRace { get; set; }
    public long BingoBlockHeight { get; set; }
    public TransactionInfoIndex? PlayTransactionInfo { get; set; }
    public TransactionInfoIndex? BingoTransactionInfo { get; set; }
}