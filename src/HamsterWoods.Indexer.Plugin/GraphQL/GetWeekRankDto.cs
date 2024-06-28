using Volo.Abp.Application.Dtos;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetWeekRankDto : PagedResultRequestDto
{
    public string SeasonId { get; set; }
    public int Week { get; set; }
}