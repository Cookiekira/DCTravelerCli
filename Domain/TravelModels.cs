using System.Text.Json.Nodes;

namespace DCTravelerCli.Domain;

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

public sealed record ActiveTravelOrder(
    string OrderId,
    string? RoleId,
    string RoleName,
    SourceRegion HomeRegion,
    SourceWorld HomeWorld,
    SourceRegion CurrentRegion,
    SourceWorld CurrentWorld,
    string? StatusDescription);

public sealed record CharacterSelection(
    string RoleId,
    string RoleName,
    SourceRegion HomeRegion,
    SourceWorld HomeWorld,
    Character? DirectCharacter,
    ActiveTravelOrder? ActiveTravelOrder)
{
    public bool RequiresReturnHome => ActiveTravelOrder is not null;

    public SourceRegion CurrentRegion => ActiveTravelOrder?.CurrentRegion ?? HomeRegion;

    public SourceWorld CurrentWorld => ActiveTravelOrder?.CurrentWorld ?? HomeWorld;
}

public sealed record AvailableTarget(TargetRegion Region, TargetWorld World);

public sealed record TravelSelection(Character Character, AvailableTarget Target);

public sealed record ReturnThenTravelSelection(ActiveTravelOrder Order, AvailableTarget Target);

public sealed record ReturnHomeSelection(
    ActiveTravelOrder Order,
    SourceRegion CurrentRegion,
    SourceWorld CurrentWorld);

public sealed record TravelOrder(string OrderId);

public sealed record ReturnHomeOrder(string? OrderId, string? Message);

public sealed record ReturnHomeResult(ActiveTravelOrder Order, ReturnHomeOrder ReturnOrder);

public sealed record OrderStatusSnapshot(MigrationStatus Status, string? Message);

public sealed record MigrationOrderSummary(
    string OrderId,
    int MigrationType,
    int MigrationStatus,
    int TravelStatus,
    string? StatusDescription);

public sealed record DiscoveryFailure(SourceRegion Region, SourceWorld World, string Message);

public sealed record CharacterDiscoveryResult(
    IReadOnlyList<Character> Characters,
    IReadOnlyList<DiscoveryFailure> Failures);

public sealed record CharacterSelectionCatalog(
    IReadOnlyList<CharacterSelection> Characters,
    IReadOnlyList<DiscoveryFailure> DiscoveryFailures,
    string? ActiveTravelOrderFailure);

public sealed record LoginProbe(bool IsLoggedIn, string? DisplayAccount);
