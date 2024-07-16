using AElfIndexer.Client.Handlers;
using HamsterWoods.Indexer.Plugin.Entities;

namespace HamsterWoods.Indexer.Plugin.Processors;

public interface IUserBalanceProvider
{
    public Task SaveUserBalanceAsync(string symbol, string address, long amount, LogEventContext context);

    public Task<List<UserBalanceDto>> QueryUserBalanceBySymbolsAsync(string chainId, List<string> symbols, string address);
    
    public Task<UserBalanceIndex> QueryUserBalanceByIdAsync(string userBalanceId, string chainId);
    
}