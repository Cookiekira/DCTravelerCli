using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DCTravelCli.Domain;
using DCTravelCli.Serialization;
using DCTravelCli.Services.OfficialDtos;

namespace DCTravelCli.Services;

public sealed class OfficialTravelClient : ITravelApi
{
    private readonly HttpClientHandler handler;
    private readonly HttpClient httpClient;
    private bool migrationOrdersInitialized;

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
                GetResponseTypeInfo<LoginProbeData>(),
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
            GetResponseTypeInfo<GroupListData>(),
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
            GetResponseTypeInfo<RoleListData>(),
            cancellationToken);

        return OfficialResponseParser.ToCharacters(response.RoleList, sourceRegion, sourceWorld);
    }

    public async Task<IReadOnlyList<TargetRegion>> GetTargetRegionsAsync(
        SourceRegion sourceRegion,
        SourceWorld sourceWorld,
        CancellationToken cancellationToken)
    {
        var response = await GetOkAsync<GroupListData>(
            "/api/orderserivce/queryGroupListTravelTarget",
            new Dictionary<string, string?>
            {
                ["appId"] = OfficialEndpoints.AppId.ToString(),
                ["areaId"] = sourceRegion.AreaId.ToString(),
                ["groupId"] = sourceWorld.GroupId.ToString()
            },
            GetResponseTypeInfo<GroupListData>(),
            cancellationToken);

        return OfficialResponseParser.ToTargetRegions(response.GroupList);
    }

    public async Task<IReadOnlyList<MigrationOrderSummary>> GetMigrationOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await GetMigrationOrderElementsAsync(cancellationToken);
        return OfficialResponseParser.ToMigrationOrders(orders);
    }

    public async Task<IReadOnlyList<ActiveTravelOrder>> GetActiveTravelOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await GetMigrationOrderElementsAsync(cancellationToken);
        return OfficialResponseParser.ToActiveTravelOrders(orders);
    }

    public async Task<IReadOnlyList<SourceRegion>> GetReturnSourceRegionsAsync(CancellationToken cancellationToken)
    {
        var response = await GetOkAsync<GroupListData>(
            "/api/gmallgateway/queryGroupListCrossSource",
            new Dictionary<string, string?>
            {
                ["appId"] = OfficialEndpoints.AppId.ToString()
            },
            GetResponseTypeInfo<GroupListData>(),
            cancellationToken);

        return OfficialResponseParser.ToSourceRegions(response.GroupList);
    }

    public async Task<ReturnHomeOrder> SubmitReturnHomeAsync(
        ReturnHomeSelection selection,
        CancellationToken cancellationToken)
    {
        var response = await GetOkAsync<TravelBackData>(
            "/api/orderserivce/travelBack",
            new Dictionary<string, string?>
            {
                ["travelOrderId"] = selection.Order.OrderId,
                ["groupId"] = selection.CurrentWorld.GroupId.ToString(),
                ["groupCode"] = selection.CurrentWorld.GroupCode,
                ["groupName"] = selection.CurrentWorld.GroupName
            },
            GetResponseTypeInfo<TravelBackData>(),
            cancellationToken);

        if (response.ResultCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(response.ResultMessage)
                ? $"官网返回接口返回错误 {response.ResultCode}。"
                : $"官网返回接口返回错误 {response.ResultCode}：{response.ResultMessage}";
            throw new OfficialApiException(response.ResultCode, message);
        }

        return new ReturnHomeOrder(response.OrderId, response.ResultMessage);
    }

    public async Task<TravelOrder> SubmitTravelOrderAsync(
        TravelSelection selection,
        CancellationToken cancellationToken)
    {
        var character = selection.Character;
        var target = selection.Target;
        var roleList = $"[{character.OfficialPayload.ToJsonString()}]";

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
            GetResponseTypeInfo<TravelOrderData>(),
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
            GetResponseTypeInfo<OrderStatusData>(),
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
            GetResponseTypeInfo<JsonElement>(),
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
        JsonTypeInfo<OfficialResponse<T>> responseJsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var response = await GetRawAsync(path, parameters, responseJsonTypeInfo, cancellationToken);
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

    private async Task<IReadOnlyList<JsonElement>> GetMigrationOrderElementsAsync(CancellationToken cancellationToken)
    {
        const int pageSize = 20;
        var orders = new List<JsonElement>();
        var pageIndex = 1;
        var totalPages = 1;

        await EnsureMigrationOrdersInitializedAsync(cancellationToken);

        while (pageIndex <= totalPages)
        {
            var response = await GetOkAsync<MigrationOrdersData>(
                "/api/orderserivce/queryMigrationOrders",
                new Dictionary<string, string?>
                {
                    ["appId"] = OfficialEndpoints.AppId.ToString(),
                    ["pageIndex"] = pageIndex.ToString(),
                    ["pageNum"] = pageSize.ToString()
                },
                GetResponseTypeInfo<MigrationOrdersData>(),
                cancellationToken);

            orders.AddRange(OfficialResponseParser.ReadElements(response.OrderList));
            totalPages = Math.Max(1, response.TotalPageNum);
            pageIndex++;
        }

        return orders;
    }

    private async Task EnsureMigrationOrdersInitializedAsync(CancellationToken cancellationToken)
    {
        if (migrationOrdersInitialized)
        {
            return;
        }

        await GetRawAsync<JsonElement>(
            "/api/orderserivce/pageInit",
            new Dictionary<string, string?>
            {
                ["migrationType"] = "0"
            },
            GetResponseTypeInfo<JsonElement>(),
            cancellationToken);
        migrationOrdersInitialized = true;
    }

    private async Task<OfficialResponse<T>> GetRawAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?> parameters,
        JsonTypeInfo<OfficialResponse<T>> responseJsonTypeInfo,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path, parameters));
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"官网接口 HTTP {(int)response.StatusCode}：{content}");
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync(contentStream, responseJsonTypeInfo, cancellationToken)
            ?? throw new InvalidOperationException("官网接口返回的 JSON 无法解析。");
    }

    private static JsonTypeInfo<OfficialResponse<T>> GetResponseTypeInfo<T>()
    {
        return (JsonTypeInfo<OfficialResponse<T>>?)DCTravelJsonSerializerContext.Default.GetTypeInfo(typeof(OfficialResponse<T>))
            ?? throw new InvalidOperationException($"未生成 {typeof(OfficialResponse<T>)} 的 JSON 元数据。");
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
