namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class RankDto
{
    public string CaAddress { get; set; }
    public long Score { get; set; }
    public string Symbol { get; set; } = "ACORNS";
    public int Decimals { get; set; } = 8;
    public int Rank { get; set; }
}