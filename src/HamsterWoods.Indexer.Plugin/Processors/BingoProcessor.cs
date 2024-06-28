using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Contracts.HamsterWoodsContract;
using HamsterWoods.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace HamsterWoods.Indexer.Plugin.Processors;

public class BingoProcessor : HamsterWoodsProcessorBase<Picked>
{
    private readonly IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> _gameInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo>
        _rankSeasonIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo>
        _rankWeekUserIndexRepository;

    private readonly GameInfoOption _gameInfoOption;
    private readonly ILogger<AElfLogEventProcessorBase<Picked, TransactionInfo>> _logger;

    public BingoProcessor(
        ILogger<AElfLogEventProcessorBase<Picked, TransactionInfo>> logger,
        IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameInfoIndexRepository,
        IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonIndexRepository,
        IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserIndexRepository,
        IOptionsSnapshot<GameInfoOption> gameInfoOption,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IObjectMapper objectMapper
    ) : base(logger, objectMapper, contractInfoOptions)
    {
        _logger = logger;
        _gameInfoIndexRepository = gameInfoIndexRepository;
        _rankSeasonIndexRepository = rankSeasonIndexRepository;
        _rankWeekUserIndexRepository = rankWeekUserIndexRepository;
        _gameInfoOption = gameInfoOption.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).BeangoTownAddress;
    }

    protected override async Task HandleEventAsync(Picked eventValue, LogEventContext context)
    {
        _logger.LogDebug("Bingoed HandleEventAsync BlockHeight:{BlockHeight} TransactionId:{TransactionId}",
            context.BlockHeight, context.TransactionId);

        RankSeasonConfigIndex seasonConfigRankIndex = null;
        if (!string.IsNullOrEmpty(_gameInfoOption.Id))
        {
            seasonConfigRankIndex = await SaveGameConfigInfoAsync(context);
        }

        var weekNum = SeasonWeekUtil.GetRankWeekNum(seasonConfigRankIndex, context.BlockTime);
        await SaveGameIndexAsync(eventValue, context, weekNum, eventValue.IsRace);
        _logger.LogDebug(" SaveGameIndexAsync Success  TransactionId:{TransactionId}",
            context.TransactionId);
        //await SaveRankWeekUserIndexAsync(eventValue, context, weekNum, weekNum);
    }

    private async Task SaveRankWeekUserIndexAsync(Picked eventValue, LogEventContext context, WeekInfo weekInfo)
    {
        // day of week 1 no, suspend?
        var dayOfWeek = 2;
        if (!eventValue.IsRace)
        {
            return;
        }
        
        // id start_date-end_date - dayofweed,
        //context.BlockTime
        var rankWeekUserId = IdGenerateHelper.GenerateId(eventValue.PlayerAddress.ToBase58());
        var rankWeekUserIndex =
            await _rankWeekUserIndexRepository.GetFromBlockStateSetAsync(rankWeekUserId, context.ChainId);
        if (rankWeekUserIndex == null)
        {
            rankWeekUserIndex = new UserWeekRankIndex()
            {
                Id = rankWeekUserId,
                WeekOfYear = weekInfo.WeekOfYear,
                CaAddress = AddressUtil.ToFullAddress(eventValue.PlayerAddress.ToBase58(), context.ChainId),
                UpdateTime = context.BlockTime,
                SumScore = eventValue.Score,
                RankBeginTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.RankBeginTime),
                RankEndTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.RankEndTime),
                ShowBeginTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.ShowBeginTime),
                ShowEndTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.ShowEndTime),
                Rank = HamsterWoodsIndexerConstants.UserDefaultRank,
                IsRace = eventValue.IsRace
            };
        }
        else
        {
            rankWeekUserIndex.SumScore += eventValue.Score;
            rankWeekUserIndex.UpdateTime = context.BlockTime;
        }

        ObjectMapper.Map(context, rankWeekUserIndex);
        await _rankWeekUserIndexRepository.AddOrUpdateAsync(rankWeekUserIndex);
    }

    private async Task SaveGameIndexAsync(Picked eventValue, LogEventContext context,
        int weekOfYear, bool isRace)
    {
        var feeAmount = GetFeeAmount(context.ExtraProperties);
        var gameIndex = new GameIndex
        {
            Id = context.TransactionId,
            CaAddress = AddressUtil.ToFullAddress(eventValue.PlayerAddress.ToBase58(), context.ChainId),
            WeekOfYear = weekOfYear,
            IsRace = isRace,
            BingoTransactionInfo = new TransactionInfoIndex()
            {
                TransactionId = context.TransactionId,
                TriggerTime = context.BlockTime,
                TransactionFee = feeAmount
            }
        };
        ObjectMapper.Map(eventValue, gameIndex);
        ObjectMapper.Map(context, gameIndex);
        await _gameInfoIndexRepository.AddOrUpdateAsync(gameIndex);
    }

    private async Task<RankSeasonConfigIndex> SaveGameConfigInfoAsync(LogEventContext context)
    {
        var rankSeasonIndex = SeasonWeekUtil.ConvertRankSeasonIndex(_gameInfoOption);
        ObjectMapper.Map(context, rankSeasonIndex);
        await _rankSeasonIndexRepository.AddOrUpdateAsync(rankSeasonIndex);
        return rankSeasonIndex;
    }
}