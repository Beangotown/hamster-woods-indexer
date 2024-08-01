namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class ScoreInfosDto
{
    public string CaAddress { get; set; }
    public long SumScore { get; set; }
    public int Decimals { get; set; } = 8;
    public string Symbol { get; set; } = "ACORNS";
}