using System.Text.Json;
using System.Text.Json.Serialization;

namespace DCTravelerCli.Services.OfficialDtos;

public sealed record GroupListData
{
    [JsonPropertyName("groupList")]
    public JsonElement GroupList { get; init; }
}
