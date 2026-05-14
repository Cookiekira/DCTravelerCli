using System.Text.Json.Serialization;

namespace DCTravelCli.Services.OfficialDtos;

public sealed record WorldDto
{
    [JsonPropertyName("groupId")]
    public int GroupId { get; init; }

    [JsonPropertyName("groupCode")]
    public string? GroupCode { get; init; }

    [JsonPropertyName("groupName")]
    public string? GroupName { get; init; }

    [JsonPropertyName("state")]
    public int? State { get; init; }

    [JsonPropertyName("queueTime")]
    public int? QueueTime { get; init; }
}
