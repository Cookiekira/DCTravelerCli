using System.Text.Json.Serialization;

namespace DCTravelCli.Services.OfficialDtos;

public sealed record LoginProbeData
{
    [JsonPropertyName("displayAccount")]
    public string? DisplayAccount { get; init; }
}
