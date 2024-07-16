namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class PurchaseResultDto
{
    public List<PurchaseDto> BuyChanceList { get; set; }
}

public class PurchaseDto
{
    public string Id { get; set; }
    public string CaAddress { get; set; }
    public int Chance { get; set; }
    public long Cost { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public long TranscationFee { get; set; }
    public TransactionInfoDto? TransactionInfo { get; set; }
}