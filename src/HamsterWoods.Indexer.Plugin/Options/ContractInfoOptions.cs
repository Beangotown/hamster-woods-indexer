namespace HamsterWoods.Indexer.Plugin;

public class ContractInfoOptions
{
    public List<ContractInfo> ContractInfos { get; set; }
}

public class ContractInfo
{
    public string ChainId { get; set; }
    public string HamsterWoodsAddress { get; set; }
    public string TokenContractAddress { get; set; }
}