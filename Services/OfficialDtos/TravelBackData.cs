using System.Text.Json.Serialization;

namespace DCTravelCli.Services.OfficialDtos;

public sealed record TravelBackData
{
    [JsonPropertyName("resultCode")]
    public int ResultCode { get; init; }

    [JsonPropertyName("resultMsg")]
    public string? ResultMessage { get; init; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }
}
