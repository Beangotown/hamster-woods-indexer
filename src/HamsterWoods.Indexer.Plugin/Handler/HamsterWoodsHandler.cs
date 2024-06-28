using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace HamsterWoods.Indexer.Plugin.Handler;

public class HamsterWoodsHandler : TransactionDataHandler
{
    public HamsterWoodsHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<TransactionInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IEnumerable<IAElfLogEventProcessor<TransactionInfo>> processors,
        ILogger<HamsterWoodsHandler> logger) : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider,
        dAppDataProvider, blockStateSetProvider, dAppDataIndexManagerProvider, processors, logger)
    {
    }

    protected override Task ProcessTransactionsAsync(List<TransactionInfo> transactions)
    {
        return Task.CompletedTask;
    }
}