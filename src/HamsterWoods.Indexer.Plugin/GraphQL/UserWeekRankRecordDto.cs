namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class UserWeekRankRecordDto
{
    public List<RankRecordDto> RankRecordList { get; set; }
}

public class RankRecordDto
{
    public string CaAddress { get; set; }
    public long SumScore { get; set; }
    public string Symbol { get; set; } = "ACORNS";
    public int Decimals { get; set; } = 8;
    public int WeekNum { get; set; }
    public DateTime UpdateTime { get; set; }
}