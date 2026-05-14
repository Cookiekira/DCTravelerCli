using System.Text.Json.Serialization;

namespace DCTravelCli.Services.OfficialDtos;

public sealed record TravelOrderData
{
    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }
}
