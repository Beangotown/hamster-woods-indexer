namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetScoreInfosDto
{
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<string> CaAddressList { get; set; }
}