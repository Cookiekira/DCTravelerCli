using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DCTravelCli.Domain;
using DCTravelCli.Services.OfficialDtos;

namespace DCTravelCli.Services;

public sealed class OfficialTravelClient : ITravelApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClientHandler handler;
    private readonly HttpClient httpClient;

    public OfficialTravelClient(OfficialSession session)
    {
        handler = new HttpClientHandler
        {
            CookieContainer = session.Cookies,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All
        };
        httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = OfficialEndpoints.BaseUri,
            Timeout = TimeSpan.FromSeconds(30)
        };
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        httpClient.DefaultRequestHeaders.Referrer = OfficialEndpoints.TravelUri;
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125 Safari/537.36");
    }

    public async Task<LoginProbe> ProbeLoginAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await GetRawAsync<LoginProbeData>(
                "/api/orderserivce/pageInit",
                new Dictionary<string, string?>
                {
                    ["migrationType"] = OfficialEndpoints.MigrationType.ToString()
                },
                cancellationToken);

            return response.ReturnCode == 0
                ? new LoginProbe(true, response.Data?.DisplayAccount)
                : new LoginProbe(false, null);
        }
        catch
        {
            return new LoginProbe(false, null);
        }
    }

    public async Task<IReadOnlyList<SourceRegion>> GetSourceRegionsAsync(CancellationToken cancellationToken)
    {
        var response = await GetOkAsync<GroupListData>(
            "/api/orderserivce/queryGroupListTravelSource",
            new Dictionary<string, string?>
            {
                ["appId"] = OfficialEndpoints.AppId.ToString()
            },
            cancellationToken);

        return OfficialResponseParser.ToSourceRegions(response.GroupList);
    }

    public async Task<IReadOnlyList<Character>> GetCharactersAsync(
        SourceRegion sourceRegion,
        SourceWorld sourceWorld,
        CancellationToken cancellationToken)
    {
        var response = await GetOkAsync<RoleListData>(
            "/api/gmallgateway/queryRoleList4Migration",
            new Dictionary<string, string?>
            {
                ["appId"] = OfficialEndpoints.AppId.ToString(),
                ["areaId"] = sourceRegion.AreaId.ToString(),
                ["groupId"] = sourceWorld.GroupId.ToString()
            },
            cancellationToken);

        return OfficialResponseParser.ToCharacters(response.RoleList, sourceRegion, sourceWorld);
    }

    public async Task<IReadOnlyList<TargetRegion>> GetTargetRegionsAsync(
        Character character,
        CancellationToken cancellationToken)
    {
        var response = await GetOkAsync<GroupListData>(
            "/api/orderserivce/queryGroupListTravelTarget",
            new Dictionary<string, string?>
            {
                ["appId"] = OfficialEndpoints.AppId.ToString(),
                ["areaId"] = character.SourceRegion.AreaId.ToString(),
                ["groupId"] = character.SourceWorld.GroupId.ToString()
            },
            cancellationToken);

        return OfficialResponseParser.ToTargetRegions(response.GroupList);
    }

    public async Task<TravelOrder> SubmitTravelOrderAsync(
        TravelSelection selection,
        CancellationToken cancellationToken)
    {
        var character = selection.Character;
        var target = selection.Target;
        var roleList = $"[{character.OfficialPayload.ToJsonString(JsonOptions)}]";

        var response = await GetOkAsync<TravelOrderData>(
            "/api/orderserivce/travelOrder",
            new Dictionary<string, string?>
            {
                ["appId"] = OfficialEndpoints.AppId.ToString(),
                ["areaId"] = character.SourceRegion.AreaId.ToString(),
                ["areaName"] = character.SourceRegion.AreaName,
                ["groupId"] = character.SourceWorld.GroupId.ToString(),
                ["groupCode"] = character.SourceWorld.GroupCode,
                ["groupName"] = character.SourceWorld.GroupName,
                ["productId"] = "1",
                ["productNum"] = "1",
                ["migrationType"] = OfficialEndpoints.MigrationType.ToString(),
                ["targetArea"] = target.Region.AreaId.ToString(),
                ["targetAreaName"] = target.Region.AreaName,
                ["targetGroupId"] = target.World.GroupId.ToString(),
                ["targetGroupCode"] = target.World.GroupCode,
                ["targetGroupName"] = target.World.GroupName,
                ["roleList"] = roleList,
                ["isMigrationTimes"] = "0"
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.OrderId))
        {
            throw new OfficialApiException(0, "下单成功但官网没有返回订单号。");
        }

        return new TravelOrder(response.OrderId);
    }

    public async Task<OrderStatusSnapshot> GetOrderStatusAsync(
        TravelOrder order,
        CancellationToken cancellationToken)
    {
        var response = await GetOkAsync<OrderStatusData>(
            "/api/gmallgateway/queryOrderStatus",
            new Dictionary<string, string?>
            {
                ["orderId"] = order.OrderId
            },
            cancellationToken);

        return new OrderStatusSnapshot((MigrationStatus)response.MigrationStatus, response.MigrationMessage);
    }

    public async Task ConfirmOrderAsync(
        TravelOrder order,
        bool confirm,
        CancellationToken cancellationToken)
    {
        var response = await GetRawAsync<JsonElement>(
            "/api/gmallgateway/migrationConfirmOrder",
            new Dictionary<string, string?>
            {
                ["orderId"] = order.OrderId,
                ["confirmType"] = confirm ? "1" : "0"
            },
            cancellationToken);

        if (response.ReturnCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(response.ReturnMessage)
                ? $"官网接口返回错误 {response.ReturnCode}。"
                : $"官网接口返回错误 {response.ReturnCode}：{response.ReturnMessage}";
            throw new OfficialApiException(response.ReturnCode, message);
        }
    }

    public void Dispose()
    {
        httpClient.Dispose();
        handler.Dispose();
    }

    private async Task<T> GetOkAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?> parameters,
        CancellationToken cancellationToken)
    {
        var response = await GetRawAsync<T>(path, parameters, cancellationToken);
        if (response.ReturnCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(response.ReturnMessage)
                ? $"官网接口返回错误 {response.ReturnCode}。"
                : $"官网接口返回错误 {response.ReturnCode}：{response.ReturnMessage}";
            throw new OfficialApiException(response.ReturnCode, message);
        }

        return response.Data
            ?? throw new OfficialApiException(response.ReturnCode, "官网接口没有返回 data。");
    }

    private async Task<OfficialResponse<T>> GetRawAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?> parameters,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path, parameters));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"官网接口 HTTP {(int)response.StatusCode}：{content}");
        }

        return JsonSerializer.Deserialize<OfficialResponse<T>>(content, JsonOptions)
            ?? throw new InvalidOperationException("官网接口返回的 JSON 无法解析。");
    }

    private static Uri BuildUri(string path, IReadOnlyDictionary<string, string?> parameters)
    {
        var builder = new UriBuilder(new Uri(OfficialEndpoints.BaseUri, path));
        builder.Query = string.Join(
            "&",
            parameters.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? string.Empty)}"));
        return builder.Uri;
    }
}
