using Volo.Abp.Application.Dtos;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetRankRecordsDto: PagedResultRequestDto
{
    public int WeekNum { get; set; }
}