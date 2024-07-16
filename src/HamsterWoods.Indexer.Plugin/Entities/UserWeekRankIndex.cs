using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace HamsterWoods.Indexer.Plugin.Entities;

public class UserWeekRankIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CaAddress { get; set; }
    public int WeekNum { get; set; }
    public long SumScore { get; set; }
    public DateTime UpdateTime { get; set; }
    public int Rank { get; set; }
    public bool IsRace { get; set; }
    public ScoreTokenInfo ScoreTokenInfo { get; set; }
}