using System.Text.Json;
using System.Text.Json.Serialization;

namespace DCTravelerCli.Services.OfficialDtos;

public sealed record RegionDto
{
    [JsonPropertyName("areaId")]
    public int AreaId { get; init; }

    [JsonPropertyName("areaName")]
    public string? AreaName { get; init; }

    [JsonPropertyName("state")]
    public int? State { get; init; }

    [JsonPropertyName("groups")]
    public JsonElement Groups { get; init; }
}
