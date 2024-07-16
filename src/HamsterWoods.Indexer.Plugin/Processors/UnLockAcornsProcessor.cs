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

public class UnLockAcornsProcessor : AElfLogEventProcessorBase<AcornsUnlocked, TransactionInfo>
{
    private readonly IAElfIndexerClientEntityRepository<UnLockAcornsIndex, TransactionInfo>
        _unlockAcornsIndexRepository;

    private readonly ILogger<UnLockAcornsProcessor> _unLockAcornsLogger;
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ScoreTokenOptions _scoreTokenOptions;

    public UnLockAcornsProcessor(
        ILogger<AElfLogEventProcessorBase<AcornsUnlocked, TransactionInfo>> logger,
        ILogger<UnLockAcornsProcessor> unLockAcornsLogger,
        IAElfIndexerClientEntityRepository<UnLockAcornsIndex, TransactionInfo> unlockAcornsIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IOptionsSnapshot<ScoreTokenOptions> scoreTokenOptions,
        IObjectMapper objectMapper) : base(logger)
    {
        _unLockAcornsLogger = unLockAcornsLogger;
        _unlockAcornsIndexRepository = unlockAcornsIndexRepository;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _scoreTokenOptions = scoreTokenOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).HamsterWoodsAddress;
    }


    protected override async Task HandleEventAsync(AcornsUnlocked eventValue, LogEventContext context)
    {
        _unLockAcornsLogger.LogDebug("AcornsUnlocked BlockHeight:{BlockHeight} TransactionId:{TransactionId}",
            context.BlockHeight, context.TransactionId);

        var feeAmount = GetFeeAmount(context.ExtraProperties);
        var unLockAcornsIndex = new UnLockAcornsIndex
        {
            Id = IdGenerateHelper.GenerateId(eventValue.To.ToBase58(), eventValue.WeekNum.ToString(),
                context.TransactionId),
            CaAddress = AddressUtil.ToFullAddress(eventValue.To.ToBase58(), context.ChainId),
            FromAddress = AddressUtil.ToFullAddress(eventValue.From.ToBase58(), context.ChainId),
            Chainid = context.ChainId,
            WeekNum = eventValue.WeekNum,
            Amount = eventValue.Amount,
            Symbol = eventValue.Symbol,
            BlockTime = context.BlockTime,
            TransactionInfo = new TransactionInfoIndex()
            {
                TransactionId = context.TransactionId,
                TriggerTime = context.BlockTime,
                TransactionFee = feeAmount
            }
        };

        _objectMapper.Map(context, unLockAcornsIndex);
        await _unlockAcornsIndexRepository.AddOrUpdateAsync(unLockAcornsIndex);

        _unLockAcornsLogger.LogDebug("Save AcornsUnlocked Success TransactionId:{TransactionId}",
            context.TransactionId);
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

    protected Dictionary<string, long> GetTransactionFee(Dictionary<string, string> extraProperties)
    {
        var feeMap = new Dictionary<string, long>();
        if (extraProperties.TryGetValue("TransactionFee", out var transactionFee))
        {
            _unLockAcornsLogger.LogDebug("TransactionFee {Fee}", transactionFee);
            feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                     new Dictionary<string, long>();
        }

        if (extraProperties.TryGetValue("ResourceFee", out var resourceFee))
        {
            _unLockAcornsLogger.LogDebug("ResourceFee {Fee}", resourceFee);
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
}