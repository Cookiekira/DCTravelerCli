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

    public static IReadOnlyList<MigrationOrderSummary> ToMigrationOrders(IReadOnlyList<JsonElement> orders)
    {
        var summaries = new List<MigrationOrderSummary>(orders.Count);

        foreach (var order in orders)
        {
            var orderId = ReadString(order, "orderId");
            if (string.IsNullOrWhiteSpace(orderId))
            {
                continue;
            }

            summaries.Add(new MigrationOrderSummary(
                orderId,
                ReadInt(order, "migrationType") ?? -1,
                ReadInt(order, "migrationStatus") ?? -1,
                ReadInt(order, "travelStatus") ?? -1,
                ReadString(order, "migrationStatusDesc")));
        }

        return summaries;
    }

    public static IReadOnlyList<ActiveTravelOrder> ToActiveTravelOrders(IReadOnlyList<JsonElement> orders)
    {
        var activeOrders = new List<ActiveTravelOrder>();

        foreach (var order in orders)
        {
            if (!IsActiveTravelOrder(order))
            {
                continue;
            }

            var orderId = ReadString(order, "orderId");
            var homeAreaId = ReadInt(order, "areaId");
            var homeGroupId = ReadInt(order, "groupId");
            var currentAreaId = ReadInt(order, "targetAreaId") ?? ReadInt(order, "targetArea");
            var currentGroupId = ReadInt(order, "targetGroupId");

            if (string.IsNullOrWhiteSpace(orderId) ||
                homeAreaId is null ||
                homeGroupId is null ||
                currentAreaId is null ||
                currentGroupId is null)
            {
                continue;
            }

            var detail = ReadOrderDetail(order);
            var roleName =
                detail is not null ? ReadString(detail.Value, "roleName") : null;
            roleName ??= ReadString(order, "roleName") ?? "未知角色";

            var roleId =
                detail is not null ? ReadString(detail.Value, "roleId") ?? ReadString(detail.Value, "id") : null;
            roleId ??= ReadString(order, "roleId") ?? ReadString(order, "id");

            var homeRegion = new SourceRegion(
                homeAreaId.Value,
                ReadString(order, "areaName") ?? homeAreaId.Value.ToString(),
                []);
            var homeWorld = new SourceWorld(
                homeGroupId.Value,
                ReadString(order, "groupCode") ?? homeGroupId.Value.ToString(),
                ReadString(order, "groupName") ?? homeGroupId.Value.ToString());
            var currentRegion = new SourceRegion(
                currentAreaId.Value,
                ReadString(order, "targetAreaName") ?? currentAreaId.Value.ToString(),
                []);
            var currentWorld = new SourceWorld(
                currentGroupId.Value,
                ReadString(order, "targetGroupCode") ?? currentGroupId.Value.ToString(),
                ReadString(order, "targetGroupName") ?? currentGroupId.Value.ToString());

            activeOrders.Add(new ActiveTravelOrder(
                orderId,
                string.IsNullOrWhiteSpace(roleId) ? null : roleId,
                roleName,
                homeRegion,
                homeWorld,
                currentRegion,
                currentWorld,
                ReadString(order, "migrationStatusDesc")));
        }

        return activeOrders
            .OrderBy(order => order.RoleName, StringComparer.Ordinal)
            .ThenBy(order => order.HomeRegion.AreaName, StringComparer.Ordinal)
            .ThenBy(order => order.HomeWorld.GroupName, StringComparer.Ordinal)
            .ToArray();
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

    private static bool IsActiveTravelOrder(JsonElement order)
    {
        var migrationType = ReadInt(order, "migrationType");
        if (migrationType != OfficialEndpoints.MigrationType)
        {
            return false;
        }

        var migrationStatus = ReadInt(order, "migrationStatus");
        var travelStatus = ReadInt(order, "travelStatus");
        var statusDescription = ReadString(order, "migrationStatusDesc") ?? string.Empty;

        return migrationStatus == 5 && travelStatus == 1 ||
            statusDescription.Contains("旅行中", StringComparison.Ordinal);
    }

    private static JsonElement? ReadOrderDetail(JsonElement order)
    {
        if (!order.TryGetProperty("migrationDetailList", out var detailList))
        {
            return null;
        }

        var details = ReadElements(detailList);
        return details.Count == 0 ? null : details[0];
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        var raw = ReadString(element, propertyName);
        return int.TryParse(raw, out var value) ? value : null;
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
