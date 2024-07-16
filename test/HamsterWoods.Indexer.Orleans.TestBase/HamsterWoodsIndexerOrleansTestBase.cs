using HamsterWoods.Indexer.TestBase;
using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace HamsterWoods.Indexer.Orleans.TestBase;

public abstract class HamsterWoodsIndexerOrleansTestBase<TStartupModule> : HamsterWoodsIndexerTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public HamsterWoodsIndexerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}