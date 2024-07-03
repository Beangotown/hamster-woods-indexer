namespace HamsterWoods.Indexer.Plugin;

public class GameInfoOption
{
    public string Id { get; set; }
    public int PlayerWeekShowCount { get; set; }
    public int PlayerWeekRankCount { get; set; }
    public List<WeekInfo> WeekInfos { get; set; }
}

public class WeekInfo
{
    /// <summary>
    /// The week of the year
    /// </summary>
    public int WeekOfYear { get; set; }
    public string RankBeginTime { get; set; }
    public string RankEndTime { get; set; }
    public string ShowBeginTime { get; set; }
    public string ShowEndTime { get; set; }
}