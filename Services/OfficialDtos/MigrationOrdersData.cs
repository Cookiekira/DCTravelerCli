using System.Text.Json;
using System.Text.Json.Serialization;

namespace DCTravelCli.Services.OfficialDtos;

public sealed record MigrationOrdersData
{
    [JsonPropertyName("orderlist")]
    public JsonElement OrderList { get; init; }

    [JsonPropertyName("totalPageNum")]
    public int TotalPageNum { get; init; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
}
