namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetHopCountDto
{
    public long HopCount { get; set; }
}

public class GetHopCountRequestDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Address { get; set; }
}

public class GetPurchaseCountDto
{
    public long PurchaseCount { get; set; }
}

public class GetPurchaseCountRequestDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Address { get; set; }
}