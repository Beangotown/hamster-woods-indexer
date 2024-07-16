using Volo.Abp.Application.Dtos;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetRankDto : PagedResultRequestDto
{
    public string CaAddress { get; set; }
    public int WeekNum { get; set; }
}