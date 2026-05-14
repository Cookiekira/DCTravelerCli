using System.Text.Json.Serialization;

namespace DCTravelerCli.Services.OfficialDtos;

public sealed record OrderStatusData
{
    [JsonPropertyName("migrationStatus")]
    public int MigrationStatus { get; init; }

    [JsonPropertyName("migrationMsg")]
    public string? MigrationMessage { get; init; }
}
