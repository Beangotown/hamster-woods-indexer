using Volo.Abp.Application.Dtos;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetBuyChanceRecordsDto : PagedResultRequestDto
{
    public string CaAddress { get; set; }
}