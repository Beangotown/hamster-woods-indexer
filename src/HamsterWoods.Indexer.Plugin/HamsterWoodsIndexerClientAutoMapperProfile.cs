using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using AutoMapper;
using Contracts.HamsterWoods;
using HamsterWoods.Indexer.Plugin.Entities;
using HamsterWoods.Indexer.Plugin.GraphQL;
using Volo.Abp.AutoMapper;

namespace HamsterWoods.Indexer.Plugin;

public class HamsterWoodsIndexerClientAutoMapperProfile : Profile
{
    public HamsterWoodsIndexerClientAutoMapperProfile()
    {
        CreateMap<LogEventContext, RankSeasonConfigIndex>();
        CreateMap<LogEventContext, GameIndex>();
        CreateMap<LogEventContext, PurchaseChanceIndex>();
        CreateMap<PurchaseChanceIndex, PurchaseDto>()
            .ForMember(dest => dest.Symbol, source => source.MapFrom(f => f.ScoreTokenInfo.Symbol))
            .ForMember(dest => dest.Decimals, source => source.MapFrom(f => f.ScoreTokenInfo.Decimals));
        CreateMap<LogEventContext, UserBalanceIndex>();
        CreateMap<LogEventContext, UserWeekRankIndex>();
        CreateMap<BlockInfo, UserWeekRankIndex>().Ignore(destination => destination.Id);
        CreateMap<BlockInfo, UserSeasonRankIndex>().Ignore(destination => destination.Id);
        CreateMap<BlockInfo, WeekRankTaskIndex>().Ignore(destination => destination.Id);
        CreateMap<UserWeekRankIndex, RankDto>().ForMember(destination => destination.Score,
            opt => opt.MapFrom(source => source.SumScore));

        CreateMap<UserWeekRankIndex, WeekRankDto>().ForMember(destination => destination.Score,
            opt => opt.MapFrom(source => source.SumScore))
            .ForMember(dest => dest.Symbol, source => source.MapFrom(f => f.ScoreTokenInfo.Symbol))
            .ForMember(dest => dest.Decimals, source => source.MapFrom(f => f.ScoreTokenInfo.Decimals));
        
        CreateMap<UserWeekRankIndex, RankRecordDto>()
            .ForMember(dest => dest.Symbol, source => source.MapFrom(f => f.ScoreTokenInfo.Symbol))
            .ForMember(dest => dest.Decimals, source => source.MapFrom(f => f.ScoreTokenInfo.Decimals));
        
        CreateMap<GameIndex, GameResultDto>().ForMember(destination => destination.TranscationFee,
                opt => opt.MapFrom(source =>
                    (source.PlayTransactionInfo != null ? source.PlayTransactionInfo.TransactionFee : 0).Add(
                        source.BingoTransactionInfo != null
                            ? source.BingoTransactionInfo.TransactionFee
                            : 0)))
            .ForMember(dest => dest.Decimals, source => source.MapFrom(f => f.ScoreTokenInfo.Decimals));
        CreateMap<TransactionInfoIndex, TransactionInfoDto>();
        CreateMap<UserBalanceIndex, UserBalanceResultDto>();
        CreateMap<RankWeekIndex, WeekDto>();
        CreateMap<Picked, GameIndex>().ForMember(destination => destination.Score,
            opt => opt.MapFrom(source => Convert.ToInt32(source.Score)));
        CreateMap<LogEventContext, TransactionChargedFeeIndex>();
        CreateMap<TransactionFeeCharged, TransactionChargedFeeIndex>();
    }
}