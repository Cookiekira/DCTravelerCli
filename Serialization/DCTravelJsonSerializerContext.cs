using System.Text.Json;
using System.Text.Json.Serialization;
using DCTravelCli.Infrastructure;
using DCTravelCli.Services.OfficialDtos;

namespace DCTravelCli.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(StoredSession))]
[JsonSerializable(typeof(CdpInvocation<CdpEmptyParameters>))]
[JsonSerializable(typeof(CdpInvocation<CdpNavigateParameters>))]
[JsonSerializable(typeof(CdpInvocation<CdpGetCookiesParameters>))]
[JsonSerializable(typeof(RegionDto))]
[JsonSerializable(typeof(WorldDto))]
[JsonSerializable(typeof(OfficialResponse<LoginProbeData>))]
[JsonSerializable(typeof(OfficialResponse<GroupListData>))]
[JsonSerializable(typeof(OfficialResponse<RoleListData>))]
[JsonSerializable(typeof(OfficialResponse<MigrationOrdersData>))]
[JsonSerializable(typeof(OfficialResponse<TravelBackData>))]
[JsonSerializable(typeof(OfficialResponse<TravelOrderData>))]
[JsonSerializable(typeof(OfficialResponse<OrderStatusData>))]
[JsonSerializable(typeof(OfficialResponse<JsonElement>))]
internal sealed partial class DCTravelJsonSerializerContext : JsonSerializerContext
{
}
