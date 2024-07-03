namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class WeekRankDto
{
    public int Week { get; set; }
    public string CaAddress { get; set; }
    public long Score { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public int Rank { get; set; }
}