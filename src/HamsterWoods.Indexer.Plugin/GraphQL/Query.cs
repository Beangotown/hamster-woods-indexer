using AElfIndexer.Client;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using GraphQL;
using HamsterWoods.Indexer.Plugin.Entities;
using Nest;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class Query
{
    [Name("syncState")]
    public static async Task<SyncStateDto> SyncState(
        [FromServices] IClusterClient clusterClient, [FromServices] IAElfIndexerClientInfoProvider clientInfoProvider,
        [FromServices] IObjectMapper objectMapper, GetSyncStateDto dto)
    {
        var version = clientInfoProvider.GetVersion();
        var clientId = clientInfoProvider.GetClientId();
        var blockStateSetInfoGrain =
            clusterClient.GetGrain<IBlockStateSetInfoGrain>(
                GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, dto.ChainId, version));
        var confirmedHeight = await blockStateSetInfoGrain.GetConfirmedBlockHeight(dto.FilterType);
        return new SyncStateDto
        {
            ConfirmedBlockHeight = confirmedHeight
        };
    }

    [Name("getWeekRank")]
    public static async Task<WeekRankResultDto> GetWeekRank(
        [FromServices] IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserRepository,
        [FromServices] IObjectMapper objectMapper, GetRankDto getRankDto)
    {
        var rankResultDto = new WeekRankResultDto();
        var mustQuery = new List<Func<QueryContainerDescriptor<UserWeekRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.WeekNum).Value(getRankDto.WeekNum)));

        QueryContainer Filter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await rankWeekUserRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.SumScore).Ascending(a => a.UpdateTime)
            , 100,
            0);

        int rank = 0;
        List<RankDto> rankDtos = new List<RankDto>();
        foreach (var item in result.Item2)
        {
            var rankDto = objectMapper.Map<UserWeekRankIndex, RankDto>(item);
            rankDto.Rank = ++rank;
            rankDtos.Add(rankDto);
            if (rankDto.CaAddress.Equals(getRankDto.CaAddress))
            {
                rankResultDto.SelfRank = rankDto;
            }
        }

        rankResultDto.RankingList = getRankDto.SkipCount >= rankDtos.Count
            ? new List<RankDto>()
            : rankDtos.Skip(getRankDto.SkipCount).Take(getRankDto.MaxResultCount).ToList();

        if (rankResultDto.SelfRank == null)
        {
            var id = IdGenerateHelper.GenerateId(AddressUtil.ToShortAddress(getRankDto.CaAddress), getRankDto.WeekNum);
            var userWeekRankIndex = await rankWeekUserRepository.GetAsync(id);
            rankResultDto.SelfRank = ConvertWeekRankDto(objectMapper, getRankDto.CaAddress, userWeekRankIndex);
        }

        return rankResultDto;
    }

    private static RankDto ConvertWeekRankDto(IObjectMapper objectMapper, String caAddress,
        UserWeekRankIndex? userWeekRankIndex)
    {
        if (userWeekRankIndex == null)
        {
            return new RankDto
            {
                CaAddress = caAddress,
                Score = 0,
                Rank = HamsterWoodsIndexerConstants.UserDefaultRank
            };
        }

        return objectMapper.Map<UserWeekRankIndex, RankDto>(userWeekRankIndex);
    }

    [Name("getGameHistory")]
    public static async Task<GameHisResultDto> GetGameHistoryAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, LogEventInfo> gameIndexRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<TransactionChargedFeeIndex, LogEventInfo> transactionChargeFeeRepository,
        [FromServices] IObjectMapper objectMapper, GetGameHisDto getGameHisDto)
    {
        if (string.IsNullOrEmpty(getGameHisDto.CaAddress))
        {
            return new GameHisResultDto()
            {
                GameList = new List<GameResultDto>()
            };
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(getGameHisDto.CaAddress)));

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var gameResult = await gameIndexRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.BingoBlockHeight), getGameHisDto.MaxResultCount, getGameHisDto.SkipCount
        );
        if (!gameResult.Item2.IsNullOrEmpty())
        {
            var transactionIdList = gameResult.Item2.Where(game => game.BingoTransactionInfo?.TransactionFee > 0)
                .Select(game => game.BingoTransactionInfo?.TransactionId).ToList();
            var feeQuery = new List<Func<QueryContainerDescriptor<TransactionChargedFeeIndex>, QueryContainer>>();
            feeQuery.Add(q => q.Terms(i => i.Field(f => f.TransactionId).Terms(transactionIdList)));

            QueryContainer FeeFilter(QueryContainerDescriptor<TransactionChargedFeeIndex> f) =>
                f.Bool(b => b.Must(feeQuery));

            var transactionChargeFeeResult = await transactionChargeFeeRepository.GetListAsync(FeeFilter);
            if (!gameResult.Item2.IsNullOrEmpty())
            {
                var transactionChargeFeeMap = transactionChargeFeeResult.Item2.ToDictionary(
                    item => item.TransactionId,
                    item => item.ChargingAddress);
                foreach (var gameIndex in gameResult.Item2)
                {
                    var transactionId = gameIndex.BingoTransactionInfo?.TransactionId;
                    var address = "";
                    if (transactionId != null)
                    {
                        transactionChargeFeeMap.TryGetValue(transactionId, out address);
                        if (!gameIndex.CaAddress.Equals(address))
                        {
                            gameIndex.BingoTransactionInfo.TransactionFee = 0;
                        }
                    }
                }
            }
        }

        return new GameHisResultDto()
        {
            GameList = objectMapper.Map<List<GameIndex>, List<GameResultDto>>(gameResult.Item2)
        };
    }


    public static async Task<GameBlockHeightDto> GetLatestGameByBlockHeight(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameIndexRepository,
        [FromServices] IObjectMapper objectMapper, GetLatestGameDto getLatestGameHisDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.BingoBlockHeight).GreaterThanOrEquals(getLatestGameHisDto.BlockHeight)));
        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await gameIndexRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.BingoBlockHeight), 1
        );
        GameBlockHeightDto gameBlockHeightDto = new GameBlockHeightDto();
        if (result.Item1 >= 1)
        {
            var latestGame = result.Item2[0];
            gameBlockHeightDto.BingoBlockHeight = latestGame.BingoBlockHeight;
            gameBlockHeightDto.LatestGameId = latestGame.Id;
            gameBlockHeightDto.BingoTime = latestGame.BingoTransactionInfo.TriggerTime;
            var countQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
            countQuery.Add(q => q.Term(i => i.Field(f => f.BingoBlockHeight).Value(latestGame.BingoBlockHeight)));
            QueryContainer CountFilter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(countQuery));
            var countResponse = await gameIndexRepository.CountAsync(CountFilter);
            gameBlockHeightDto.GameCount = countResponse.Count;
            return gameBlockHeightDto;
        }

        gameBlockHeightDto.BingoBlockHeight = getLatestGameHisDto.BlockHeight;
        return gameBlockHeightDto;
    }

    [Name("getGoRecords")]
    public static async Task<List<GameRecordDto>> GetGoRecordsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameRepository,
        [FromServices] IObjectMapper objectMapper, GetGoRecordDto getGoRecordDto)
    {
        var recordList = new List<GameRecordDto>();
        var skipCount = 0;
        Tuple<long, List<GameIndex>> result = null;
        do
        {
            result = await gameRepository.GetSortListAsync(null, null,
                sortFunc: s => s.Ascending(a => a.BingoTransactionInfo.TriggerTime)
                , GetGoRecordDto.MaxMaxResultCount, skipCount);

            foreach (var item in result.Item2)
            {
                if (recordList.Exists(i => i.CaAddress == item.CaAddress)) continue;
                var dto = new GameRecordDto();
                dto.Id = item.CaAddress;
                dto.CaAddress = item.CaAddress;
                dto.TriggerTime = item.BingoTransactionInfo.TriggerTime;
                recordList.Add(dto);
            }

            skipCount += GetGoRecordDto.MaxMaxResultCount;
        } while (result.Item2.Count >= GetGoRecordDto.MaxMaxResultCount);

        return recordList;
    }

    [Name("getGoCount")]
    public static async Task<GameGoCountDto> GetGoCountAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameRepository,
        GetGoDto getGoDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        if (getGoDto.CaAddressList != null)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaAddress).Terms(getGoDto.CaAddressList)));
        }

        if (getGoDto.StartTime != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.BingoTransactionInfo.TriggerTime).GreaterThanOrEquals(getGoDto.StartTime)));
        }

        if (getGoDto.EndTime != null)
        {
            mustQuery.Add(q =>
                q.DateRange(i => i.Field(f => f.BingoTransactionInfo.TriggerTime).LessThanOrEquals(getGoDto.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var gameList = new List<GameIndex>();
        var skipCount = getGoDto.SkipCount;
        Tuple<long, List<GameIndex>> result = null;
        do
        {
            result = await gameRepository.GetListAsync(Filter, skip: skipCount, limit: GetGoDto.MaxMaxResultCount);
            if (result.Item2.Count == 0) break;
            gameList.AddRange(result.Item2);
            skipCount += GetGoDto.MaxMaxResultCount;
        } while (result.Item2.Count >= GetGoDto.MaxMaxResultCount);

        var goResponse = gameList.GroupBy(g => g.CaAddress)
            .Select(group => new { caAddress = group.Key, Count = group.Count() })
            .Where(x => x.Count >= getGoDto.GoCount);

        return new GameGoCountDto
        {
            GoCount = goResponse.Count()
        };
    }

    [Name("getGameHistoryList")]
    public static async Task<GameHistoryResultDto> GetGameHistoryListAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, LogEventInfo> gameIndexRepository,
        [FromServices] IObjectMapper objectMapper, GetGameHistoryDto getGameHistoryDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        if (!getGameHistoryDto.CaAddress.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(getGameHistoryDto.CaAddress)));
        }

        if (getGameHistoryDto.BeginTime != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.BingoTransactionInfo!.TriggerTime).GreaterThanOrEquals(getGameHistoryDto.BeginTime)));
        }

        if (getGameHistoryDto.EndTime != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.BingoTransactionInfo!.TriggerTime).LessThanOrEquals(getGameHistoryDto.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var gameResult = await gameIndexRepository.GetListAsync(Filter, limit: getGameHistoryDto.MaxResultCount,
            skip: getGameHistoryDto.SkipCount
        );
        return new GameHistoryResultDto()
        {
            GameList = objectMapper.Map<List<GameIndex>, List<GameResultDto>>(gameResult.Item2)
        };
    }

    [Name("getUserBalanceList")]
    public static async Task<List<UserBalanceResultDto>> GetUserBalanceList(
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper, GetUserBalanceDto userBalanceDto)
    {
        var symbols = userBalanceDto.symbols;
        if (symbols.IsNullOrEmpty())
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(userBalanceDto.address)));

        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(userBalanceDto.chainId)));

        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Symbol).Terms(userBalanceDto.symbols)));

        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) => f.Bool(b => b.Must(mustQuery));

        var userBalanceIndexList = await repository.GetListAsync(Filter);

        return userBalanceIndexList.Item2.IsNullOrEmpty()
            ? new List<UserBalanceResultDto>()
            : objectMapper.Map<List<UserBalanceIndex>, List<UserBalanceResultDto>>(userBalanceIndexList.Item2);
    }


    [Name("getBuyChanceRecords")]
    public static async Task<PurchaseResultDto> GetBuyChanceRecordsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<PurchaseChanceIndex, LogEventInfo> purchaseIndexRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<TransactionChargedFeeIndex, LogEventInfo> transactionChargeFeeRepository,
        [FromServices] IObjectMapper objectMapper, GetBuyChanceRecordsDto getBuyChanceRecordsDto)
    {
        if (string.IsNullOrEmpty(getBuyChanceRecordsDto.CaAddress))
        {
            return new PurchaseResultDto()
            {
                BuyChanceList = new List<PurchaseDto>()
            };
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<PurchaseChanceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(getBuyChanceRecordsDto.CaAddress)));

        QueryContainer Filter(QueryContainerDescriptor<PurchaseChanceIndex> f) => f.Bool(b => b.Must(mustQuery));

        var gameResult = await purchaseIndexRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.BlockHeight), getBuyChanceRecordsDto.MaxResultCount,
            getBuyChanceRecordsDto.SkipCount
        );
        if (!gameResult.Item2.IsNullOrEmpty())
        {
            var transactionIdList = gameResult.Item2.Where(game => game.TransactionInfo?.TransactionFee > 0)
                .Select(game => game.TransactionInfo?.TransactionId).ToList();
            var feeQuery = new List<Func<QueryContainerDescriptor<TransactionChargedFeeIndex>, QueryContainer>>();
            feeQuery.Add(q => q.Terms(i => i.Field(f => f.TransactionId).Terms(transactionIdList)));

            QueryContainer FeeFilter(QueryContainerDescriptor<TransactionChargedFeeIndex> f) =>
                f.Bool(b => b.Must(feeQuery));

            var transactionChargeFeeResult = await transactionChargeFeeRepository.GetListAsync(FeeFilter);
            if (!transactionChargeFeeResult.Item2.IsNullOrEmpty())
            {
                var transactionChargeFeeMap = transactionChargeFeeResult.Item2.ToDictionary(
                    item => item.TransactionId,
                    item => item.ChargingAddress);
                foreach (var gameIndex in gameResult.Item2)
                {
                    var transactionId = gameIndex.TransactionInfo?.TransactionId;
                    var address = "";
                    if (transactionId != null)
                    {
                        transactionChargeFeeMap.TryGetValue(transactionId, out address);
                        if (!gameIndex.CaAddress.Equals(address))
                        {
                            gameIndex.TransactionInfo.TransactionFee = 0;
                        }
                    }
                }
            }
        }

        return new PurchaseResultDto()
        {
            BuyChanceList = objectMapper.Map<List<PurchaseChanceIndex>, List<PurchaseDto>>(gameResult.Item2)
        };
    }

    [Name("getWeekRankRecords")]
    public static async Task<UserWeekRankRecordDto> GetWeekRankRecords(
        [FromServices] IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserRepository,
        [FromServices] IObjectMapper objectMapper, GetRankRecordsDto getRankRecordsDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserWeekRankIndex>, QueryContainer>>();

        if (getRankRecordsDto.WeekNum > 0)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.WeekNum).Value(getRankRecordsDto.WeekNum)));
        }

        QueryContainer Filter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await rankWeekUserRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.SumScore).Ascending(a => a.UpdateTime)
            , getRankRecordsDto.MaxResultCount,
            getRankRecordsDto.SkipCount);

        return new UserWeekRankRecordDto
        {
            RankRecordList = objectMapper.Map<List<UserWeekRankIndex>, List<RankRecordDto>>(result.Item2)
        };
    }

    [Name("getUnLockedRecords")]
    public static async Task<UnLockedRecordsDto> GetUnLockedRecords(
        [FromServices] IAElfIndexerClientEntityRepository<UnLockAcornsIndex, TransactionInfo> unLockRecordRepository,
        [FromServices] IObjectMapper objectMapper, GetUnLockedRecordsDto getUnLockedRecordsDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UnLockAcornsIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(getUnLockedRecordsDto.CaAddress)));

        if (getUnLockedRecordsDto.WeekNum > 0)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.WeekNum).Value(getUnLockedRecordsDto.WeekNum)));
        }

        QueryContainer Filter(QueryContainerDescriptor<UnLockAcornsIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await unLockRecordRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.WeekNum)
            , getUnLockedRecordsDto.MaxResultCount,
            getUnLockedRecordsDto.SkipCount);

        return new UnLockedRecordsDto
        {
            UnLockRecordList = objectMapper.Map<List<UnLockAcornsIndex>, List<UnLockRecordDto>>(result.Item2)
        };
    }

    [Name("getSelfWeekRank")]
    public static async Task<RankDto> GetSelfWeekRank(
        [FromServices] IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserRepository,
        [FromServices] IObjectMapper objectMapper, GetSelfWeekRankDto getRankDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserWeekRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.WeekNum).Value(getRankDto.WeekNum)));

        QueryContainer Filter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await rankWeekUserRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.SumScore).Ascending(a => a.UpdateTime)
            , 1000,
            0);
        var userRankIndex = result.Item2.FirstOrDefault(t => t.CaAddress == getRankDto.CaAddress);
        if (userRankIndex == null)
        {
            var id = IdGenerateHelper.GenerateId(AddressUtil.ToShortAddress(getRankDto.CaAddress), getRankDto.WeekNum);
            var userWeekRankIndex = await rankWeekUserRepository.GetAsync(id);
            return ConvertWeekRankDto(objectMapper, getRankDto.CaAddress, userWeekRankIndex);
            ;
        }

        var rank = 0;
        foreach (var item in result.Item2)
        {
            if (item.CaAddress == getRankDto.CaAddress)
            {
                userRankIndex.Rank = ++rank;
                break;
            }

            ++rank;
        }

        return objectMapper.Map<UserWeekRankIndex, RankDto>(userRankIndex);
    }

    [Name("getScoreInfos")]
    public static async Task<List<ScoreInfosDto>> GetScoreInfos(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameRepository,
        GetScoreInfosDto getScoreInfosDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        if (getScoreInfosDto.CaAddressList.IsNullOrEmpty() || getScoreInfosDto.CaAddressList.Count > 100)
        {
            return new List<ScoreInfosDto>();
        }

        mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaAddress).Terms(getScoreInfosDto.CaAddressList)));
        if (getScoreInfosDto.BeginTime != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.BingoTransactionInfo.TriggerTime).GreaterThanOrEquals(getScoreInfosDto.BeginTime)));
        }

        if (getScoreInfosDto.EndTime != null)
        {
            mustQuery.Add(q =>
                q.DateRange(i =>
                    i.Field(f => f.BingoTransactionInfo.TriggerTime).LessThanOrEquals(getScoreInfosDto.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var gameList = new List<GameIndex>();
        var skipCount = 0;
        Tuple<long, List<GameIndex>> result = null;
        do
        {
            result = await gameRepository.GetListAsync(Filter, skip: skipCount, limit: GetGoDto.MaxMaxResultCount);
            if (result.Item2.Count == 0) break;
            gameList.AddRange(result.Item2);
            skipCount += GetGoDto.MaxMaxResultCount;
        } while (result.Item2.Count >= GetGoDto.MaxMaxResultCount);


        var resultDto = new List<ScoreInfosDto>();
        var gameGroup = gameList.GroupBy(g => g.CaAddress);
        foreach (var groupInfo in gameGroup)
        {
            resultDto.Add(new ScoreInfosDto()
            {
                CaAddress = groupInfo.Key,
                SumScore = groupInfo.Sum(t => t.Score)
            });
        }

        return resultDto;
    }

    [Name("getHopCount")]
    public static async Task<GetHopCountDto> GetHopCountAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameRepository,
        GetHopCountRequestDto requestDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(requestDto.Address)));

        if (requestDto.StartTime != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.BingoTransactionInfo.TriggerTime).GreaterThanOrEquals(requestDto.StartTime)));
        }

        if (requestDto.EndTime != null)
        {
            mustQuery.Add(q =>
                q.DateRange(i =>
                    i.Field(f => f.BingoTransactionInfo.TriggerTime).LessThanOrEquals(requestDto.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await gameRepository.GetListAsync(Filter, skip: 0, limit: 1);


        return new GetHopCountDto
        {
            HopCount = result.Item1
        };
    }


    [Name("getPurchaseCount")]
    public static async Task<GetPurchaseCountDto> GetPurchaseCountAsync(
        [FromServices] IAElfIndexerClientEntityRepository<PurchaseChanceIndex, TransactionInfo> purchaseRepository,
        GetPurchaseCountRequestDto requestDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PurchaseChanceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(requestDto.Address)));

        if (requestDto.StartTime != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.TransactionInfo.TriggerTime).GreaterThanOrEquals(requestDto.StartTime)));
        }

        if (requestDto.EndTime != null)
        {
            mustQuery.Add(q =>
                q.DateRange(i => i.Field(f => f.TransactionInfo.TriggerTime).LessThanOrEquals(requestDto.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<PurchaseChanceIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await purchaseRepository.GetListAsync(Filter);

        return new GetPurchaseCountDto
        {
            PurchaseCount = result.Item2.IsNullOrEmpty() ? 0 : result.Item2.Sum(t => t.Chance)
        };
    }

    [Name("getPurchaseRecords")]
    public static async Task<PurchaseResultDto> GetPurchaseRecordsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<PurchaseChanceIndex, LogEventInfo> purchaseIndexRepository,
        [FromServices] IObjectMapper objectMapper, GetPurchaseRecordsDto requestDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PurchaseChanceIndex>, QueryContainer>>();
        if (!requestDto.CaAddress.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(requestDto.CaAddress)));
        }
        
        if (requestDto.StartTime != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.TransactionInfo.TriggerTime).GreaterThanOrEquals(requestDto.StartTime)));
        }

        if (requestDto.EndTime != null)
        {
            mustQuery.Add(q =>
                q.DateRange(i => i.Field(f => f.TransactionInfo.TriggerTime).LessThanOrEquals(requestDto.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<PurchaseChanceIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result =
            await purchaseIndexRepository.GetListAsync(Filter, skip: requestDto.SkipCount,
                limit: requestDto.MaxResultCount);

        return new PurchaseResultDto()
        {
            BuyChanceList = objectMapper.Map<List<PurchaseChanceIndex>, List<PurchaseDto>>(result.Item2)
        };
    }
}