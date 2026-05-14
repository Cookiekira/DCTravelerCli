using System.Text.Json.Nodes;

namespace DCTravelCli.Domain;

public sealed record SourceRegion(int AreaId, string AreaName, IReadOnlyList<SourceWorld> Worlds, int? State = null);

public sealed record SourceWorld(int GroupId, string GroupCode, string GroupName, int? State = null);

public sealed record TargetRegion(int AreaId, string AreaName, IReadOnlyList<TargetWorld> Worlds, int? State = null);

public sealed record TargetWorld(int GroupId, string GroupCode, string GroupName, int? State = null);

public sealed record Character(
    string RoleId,
    string RoleName,
    SourceRegion SourceRegion,
    SourceWorld SourceWorld,
    JsonObject OfficialPayload);

public sealed record AvailableTarget(TargetRegion Region, TargetWorld World);

public sealed record TravelSelection(Character Character, AvailableTarget Target);

public sealed record TravelOrder(string OrderId);

public sealed record OrderStatusSnapshot(MigrationStatus Status, string? Message);

public sealed record DiscoveryFailure(SourceRegion Region, SourceWorld World, string Message);

public sealed record CharacterDiscoveryResult(
    IReadOnlyList<Character> Characters,
    IReadOnlyList<DiscoveryFailure> Failures);

public sealed record LoginProbe(bool IsLoggedIn, string? DisplayAccount);
