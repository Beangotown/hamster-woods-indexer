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

public class ChancePurchasedProcessor : AElfLogEventProcessorBase<ChancePurchased, TransactionInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ChancePurchasedProcessor> _chanceLogger;
    private readonly IAElfIndexerClientEntityRepository<PurchaseChanceIndex, TransactionInfo>
        _purchaseChanceIndexRepository;

    public ChancePurchasedProcessor(ILogger<AElfLogEventProcessorBase<ChancePurchased, TransactionInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IObjectMapper objectMapper,
        ILogger<ChancePurchasedProcessor> chanceLogger, IAElfIndexerClientEntityRepository<PurchaseChanceIndex, TransactionInfo> purchaseChanceIndexRepository) : base(logger)
    {
        _objectMapper = objectMapper;
        _chanceLogger = chanceLogger;
        _purchaseChanceIndexRepository = purchaseChanceIndexRepository;
        _contractInfoOptions = contractInfoOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).HamsterWoodsAddress;
    }

    protected override async Task HandleEventAsync(ChancePurchased eventValue, LogEventContext context)
    {
        _chanceLogger.LogDebug("ChancePurchased HandleEventAsync BlockHeight:{BlockHeight} TransactionId:{TransactionId}",
            context.BlockHeight, context.TransactionId);

        var feeAmount = GetFeeAmount(context.ExtraProperties);
        var index = new PurchaseChanceIndex
        {
            Id = IdGenerateHelper.GenerateId(context.BlockHash, context.TransactionId),
            CaAddress = AddressUtil.ToFullAddress(eventValue.PlayerAddress.ToBase58(), context.ChainId),
            Chainid = context.ChainId,
            Chance = eventValue.ChanceCount,
            Cost = eventValue.AcornsAmount,
            TransactionInfo = new TransactionInfoIndex
            {
                TransactionId = context.TransactionId,
                TriggerTime = context.BlockTime,
                TransactionFee = feeAmount
            }
        };
        
        _objectMapper.Map(eventValue, index);
        _objectMapper.Map(context, index);
        await _purchaseChanceIndexRepository.AddOrUpdateAsync(index);
    }
    
    
    protected Dictionary<string, long> GetTransactionFee(Dictionary<string, string> extraProperties)
    {
        var feeMap = new Dictionary<string, long>();
        if (extraProperties.TryGetValue("TransactionFee", out var transactionFee))
        {
            _chanceLogger.LogDebug("TransactionFee {Fee}", transactionFee);
            feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                     new Dictionary<string, long>();
        }

        if (extraProperties.TryGetValue("ResourceFee", out var resourceFee))
        {
            _chanceLogger.LogDebug("ResourceFee {Fee}", resourceFee);
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