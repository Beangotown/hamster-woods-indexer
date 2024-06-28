using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Contracts.HamsterWoodsContract;
using Nest;

namespace HamsterWoods.Indexer.Plugin.Entities;

public class GameIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string? SeasonId { get; set; }
    public long PlayBlockHeight { get; set; }
    public bool IsComplete { get; set; }
    public GridType GridType { get; set; }
    public int GridNum { get; set; }
    public int Score { get; set; }
    public int WeekOfYear { get; set; }
    public bool IsRace { get; set; }
    public long BingoBlockHeight { get; set; }

    public TransactionInfoIndex? PlayTransactionInfo { get; set; }
    public TransactionInfoIndex? BingoTransactionInfo { get; set; }
}