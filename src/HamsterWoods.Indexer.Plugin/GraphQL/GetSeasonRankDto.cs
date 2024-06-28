using Volo.Abp.Application.Dtos;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetSeasonRankDto : PagedResultRequestDto
{
    public string SeasonId { get; set; }
}