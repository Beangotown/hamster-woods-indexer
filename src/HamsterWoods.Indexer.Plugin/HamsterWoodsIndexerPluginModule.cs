using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using HamsterWoods.Indexer.Plugin.GraphQL;
using HamsterWoods.Indexer.Plugin.Handler;
using HamsterWoods.Indexer.Plugin.Processors;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace HamsterWoods.Indexer.Plugin;

[DependsOn(typeof(AElfIndexerClientModule), typeof(AbpAutoMapperModule))]
public class HamsterWoodsIndexerPluginModule : AElfIndexerClientPluginBaseModule<HamsterWoodsIndexerPluginModule,
    HamsterWoodsIndexerPluginSchema, Query>
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        var configuration = serviceCollection.GetConfiguration();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, PickedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, ChancePurchasedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, UnLockAcornsProcessor>();
        serviceCollection.AddSingleton<IBlockChainDataHandler, HamsterWoodsHandler>();

        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, CrossChainReceivedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TokenIssueLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TokenTransferProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TokenBurnedLogEventProcessor>();

        Configure<ContractInfoOptions>(configuration.GetSection("ContractInfo"));
        Configure<GameInfoOption>(configuration.GetSection("GameInfo"));
        Configure<RankInfoOption>(configuration.GetSection("RankInfo"));
        Configure<ScoreTokenOptions>(configuration.GetSection("ScoreToken"));
        serviceCollection
            .AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TransactionFeeChargedProcessor>();
    }

    protected override string ClientId => "AElfIndexer_HamsterWoods";
    protected override string Version => "";
}