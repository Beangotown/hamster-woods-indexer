namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GameHisResultDto
{
    public List<GameResultDto> GameList { get; set; }
}

public class GameHistoryResultDto : GameHisResultDto
{
}

public class GameResultDto
{
    public string Id { get; set; }
    public string CaAddress { get; set; }
    public int GridNum { get; set; }
    public long Score { get; set; }
    public int Decimals { get; set; }
    public long TranscationFee { get; set; }
    public TransactionInfoDto? PlayTransactionInfo { get; set; }
    public TransactionInfoDto? BingoTransactionInfo { get; set; }
}