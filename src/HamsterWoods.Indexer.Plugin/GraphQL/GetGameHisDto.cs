using Volo.Abp.Application.Dtos;

namespace HamsterWoods.Indexer.Plugin.GraphQL;

public class GetGameHisDto : PagedResultRequestDto
{
    public string CaAddress { get; set; }
}