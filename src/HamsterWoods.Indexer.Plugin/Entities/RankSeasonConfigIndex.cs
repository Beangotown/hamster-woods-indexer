using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace HamsterWoods.Indexer.Plugin.Entities;

public class RankSeasonConfigIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public int PlayerWeekRankCount { get; set; }
    public int PlayerWeekShowCount { get; set; }
    public List<RankWeekIndex> WeekInfos { get; set; }
}

public class RankWeekIndex : IIndexBuild
{
    /// <summary>
    /// The week of the year
    /// </summary>
    public int WeekOfYear { get; set; }
    public DateTime RankBeginTime { get; set; }
    public DateTime RankEndTime { get; set; }
    public DateTime ShowBeginTime { get; set; }
    public DateTime ShowEndTime { get; set; }
}