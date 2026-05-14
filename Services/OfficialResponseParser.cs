using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using DCTravelCli.Domain;
using DCTravelCli.Services.OfficialDtos;

namespace DCTravelCli.Services;

public static class OfficialResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static IReadOnlyList<SourceRegion> ToSourceRegions(JsonElement groupList)
    {
        return ReadArray<RegionDto>(groupList)
            .Select(region => new SourceRegion(
                region.AreaId,
                region.AreaName ?? region.AreaId.ToString(),
                ReadArray<WorldDto>(region.Groups).Select(ToSourceWorld).ToArray(),
                region.State))
            .ToArray();
    }

    public static IReadOnlyList<TargetRegion> ToTargetRegions(JsonElement groupList)
    {
        return ReadArray<RegionDto>(groupList)
            .Select(region => new TargetRegion(
                region.AreaId,
                region.AreaName ?? region.AreaId.ToString(),
                ReadArray<WorldDto>(region.Groups).Select(ToTargetWorld).ToArray(),
                region.State))
            .ToArray();
    }

    public static IReadOnlyList<Character> ToCharacters(JsonElement roleList, SourceRegion sourceRegion, SourceWorld sourceWorld)
    {
        var roles = ReadElements(roleList);
        var characters = new List<Character>(roles.Count);

        for (var i = 0; i < roles.Count; i++)
        {
            var role = roles[i];
            var roleName = ReadString(role, "roleName") ?? ReadString(role, "name") ?? "未知角色";
            var roleId = ReadString(role, "roleId") ?? ReadString(role, "id") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roleId))
            {
                continue;
            }

            var payload = JsonNode.Parse(role.GetRawText())?.AsObject()
                ?? throw new InvalidOperationException("角色数据不是 JSON 对象。");
            payload["key"] = i;

            characters.Add(new Character(roleId, roleName, sourceRegion, sourceWorld, payload));
        }

        return characters;
    }

    public static IReadOnlyList<JsonElement> ReadElements(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var raw = value.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            using var document = JsonDocument.Parse(raw);
            return ReadElements(document.RootElement).Select(element => element.Clone()).ToArray();
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().Select(element => element.Clone()).ToArray();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return [value.Clone()];
        }

        return [];
    }

    public static IReadOnlyList<T> ReadArray<T>(JsonElement value)
    {
        return ReadElements(value)
            .Select(element => element.Deserialize<T>(JsonOptions))
            .Where(item => item is not null)
            .Cast<T>()
            .ToArray();
    }

    private static SourceWorld ToSourceWorld(WorldDto world)
    {
        return new SourceWorld(
            world.GroupId,
            world.GroupCode ?? world.GroupId.ToString(),
            world.GroupName ?? world.GroupId.ToString(),
            world.State);
    }

    private static TargetWorld ToTargetWorld(WorldDto world)
    {
        return new TargetWorld(
            world.GroupId,
            world.GroupCode ?? world.GroupId.ToString(),
            world.GroupName ?? world.GroupId.ToString(),
            world.State);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }
}
