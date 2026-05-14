using System.Text.Json.Serialization;

namespace DCTravelCli.Services.OfficialDtos;

public sealed record OfficialResponse<T>
{
    [JsonPropertyName("return_code")]
    public int ReturnCode { get; init; }

    [JsonPropertyName("return_message")]
    public string? ReturnMessage { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }
}
