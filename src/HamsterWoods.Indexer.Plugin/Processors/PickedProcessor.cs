using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Contracts.HamsterWoods;
using HamsterWoods.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace HamsterWoods.Indexer.Plugin.Processors;

public class PickedProcessor : AElfLogEventProcessorBase<Picked, TransactionInfo>
{
    private readonly IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> _gameInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo>
        _rankSeasonIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo>
        _rankWeekUserIndexRepository;

    private readonly GameInfoOption _gameInfoOption;
    private readonly ILogger<PickedProcessor> _pickedLogger;
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ScoreTokenOptions _scoreTokenOptions;

    public PickedProcessor(
        ILogger<AElfLogEventProcessorBase<Picked, TransactionInfo>> logger,
        ILogger<PickedProcessor> pickedLogger,
        IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameInfoIndexRepository,
        IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonIndexRepository,
        IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserIndexRepository,
        IOptionsSnapshot<GameInfoOption> gameInfoOption,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IOptionsSnapshot<ScoreTokenOptions> scoreTokenOptions,
        IObjectMapper objectMapper) : base(logger)
    {
        _pickedLogger = pickedLogger;
        _gameInfoIndexRepository = gameInfoIndexRepository;
        _rankSeasonIndexRepository = rankSeasonIndexRepository;
        _rankWeekUserIndexRepository = rankWeekUserIndexRepository;
        _gameInfoOption = gameInfoOption.Value;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _scoreTokenOptions = scoreTokenOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).HamsterWoodsAddress;
    }

    protected override async Task HandleEventAsync(Picked eventValue, LogEventContext context)
    {
        _pickedLogger.LogDebug("Picked HandleEventAsync BlockHeight:{BlockHeight} TransactionId:{TransactionId}",
            context.BlockHeight, context.TransactionId);

        await SaveGameIndexAsync(eventValue, context, eventValue.WeekNum, eventValue.IsRace);
        await SaveRankWeekUserIndexAsync(eventValue, context, eventValue.WeekNum);
        _pickedLogger.LogDebug("Save Picked Success TransactionId:{TransactionId}",
            context.TransactionId);
    }

    private async Task SaveRankWeekUserIndexAsync(Picked eventValue, LogEventContext context, int weekNum)
    {
        var rankWeekUserId = IdGenerateHelper.GenerateId(eventValue.PlayerAddress.ToBase58(), weekNum);
        var rankWeekUserIndex =
            await _rankWeekUserIndexRepository.GetFromBlockStateSetAsync(rankWeekUserId, context.ChainId);
        if (rankWeekUserIndex == null)
        {
            rankWeekUserIndex = new UserWeekRankIndex()
            {
                Id = rankWeekUserId,
                WeekNum = weekNum,
                CaAddress = AddressUtil.ToFullAddress(eventValue.PlayerAddress.ToBase58(), context.ChainId),
                UpdateTime = context.BlockTime,
                SumScore = eventValue.Score,
                Rank = HamsterWoodsIndexerConstants.UserDefaultRank,
                IsRace = eventValue.IsRace,
                ScoreTokenInfo = new ScoreTokenInfo
                {
                    Symbol = _scoreTokenOptions.Symbol,
                    Decimals = _scoreTokenOptions.Decimals
                }
            };
        }
        else
        {
            rankWeekUserIndex.SumScore += eventValue.Score;
            rankWeekUserIndex.UpdateTime = context.BlockTime;
        }

        _objectMapper.Map(context, rankWeekUserIndex);
        await _rankWeekUserIndexRepository.AddOrUpdateAsync(rankWeekUserIndex);
    }

    private async Task SaveGameIndexAsync(Picked eventValue, LogEventContext context,
        int weekNum, bool isRace)
    {
        var feeAmount = GetFeeAmount(context.ExtraProperties);
        var gameIndex = new GameIndex
        {
            Id = IdGenerateHelper.GenerateId(context.BlockHash, context.TransactionId),
            CaAddress = AddressUtil.ToFullAddress(eventValue.PlayerAddress.ToBase58(), context.ChainId),
            Chainid = context.ChainId,
            WeekNum = weekNum,
            IsRace = isRace,
            BingoTransactionInfo = new TransactionInfoIndex()
            {
                TransactionId = context.TransactionId,
                TriggerTime = context.BlockTime,
                TransactionFee = feeAmount
            },
            ScoreTokenInfo = new ScoreTokenInfo
            {
                Symbol = _scoreTokenOptions.Symbol,
                Decimals = _scoreTokenOptions.Decimals
            }
        };
        _objectMapper.Map(eventValue, gameIndex);
        _objectMapper.Map(context, gameIndex);
        await _gameInfoIndexRepository.AddOrUpdateAsync(gameIndex);
    }

    private async Task<RankSeasonConfigIndex> SaveGameConfigInfoAsync(LogEventContext context)
    {
        var rankSeasonIndex = SeasonWeekUtil.ConvertRankSeasonIndex(_gameInfoOption);
        _objectMapper.Map(context, rankSeasonIndex);
        await _rankSeasonIndexRepository.AddOrUpdateAsync(rankSeasonIndex);
        return rankSeasonIndex;
    }

    protected Dictionary<string, long> GetTransactionFee(Dictionary<string, string> extraProperties)
    {
        var feeMap = new Dictionary<string, long>();
        if (extraProperties.TryGetValue("TransactionFee", out var transactionFee))
        {
            _pickedLogger.LogDebug("TransactionFee {Fee}", transactionFee);
            feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                     new Dictionary<string, long>();
        }

        if (extraProperties.TryGetValue("ResourceFee", out var resourceFee))
        {
            _pickedLogger.LogDebug("ResourceFee {Fee}", resourceFee);
            var resourceFeeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(resourceFee) ??
                                 new Dictionary<string, long>();
            foreach (var (symbol, fee) in resourceFeeMap)
            {
                if (feeMap.ContainsKey(symbol))
                {
                    feeMap[symbol] += fee;
                }
                else
                {
                    feeMap[symbol] = fee;
                }
            }
        }

        return feeMap;
    }

    protected long GetFeeAmount(Dictionary<string, string> extraProperties)
    {
        var feeMap = GetTransactionFee(extraProperties);
        if (feeMap.TryGetValue("ELF", out var value))
        {
            return value;
        }

        return 0;
    }
}