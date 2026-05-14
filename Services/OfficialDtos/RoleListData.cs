using System.Text.Json;
using System.Text.Json.Serialization;

namespace DCTravelerCli.Services.OfficialDtos;

public sealed record RoleListData
{
    [JsonPropertyName("roleList")]
    public JsonElement RoleList { get; init; }
}
