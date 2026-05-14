using System.Text.Json.Serialization;

namespace DCTravelerCli.Services.OfficialDtos;

public sealed record TravelOrderData
{
    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }
}
