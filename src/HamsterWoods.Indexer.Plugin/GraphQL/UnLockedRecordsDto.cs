namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class UnLockedRecordsDto
{
    public List<UnLockRecordDto> UnLockRecordList { get; set; }
}

public class UnLockRecordDto
{
    public string Id { get; set; }

    public string FromAddress { get; set; }
    public string CaAddress { get; set; }
    public string Chainid { get; set; }

    public long Amount { get; set; }
    public string? Symbol { get; set; }
    public int Decimals { get; set; } = 8;

    public int WeekNum { get; set; }
    public DateTime BlockTime { get; set; }
    public TransactionInfoDto TransactionInfo { get; set; }
}