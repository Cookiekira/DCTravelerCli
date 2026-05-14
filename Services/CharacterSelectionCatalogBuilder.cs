using DCTravelCli.Domain;

namespace DCTravelCli.Services;

public sealed class CharacterSelectionCatalogBuilder(ICharacterDiscovery characterDiscovery) : ICharacterSelectionCatalogBuilder
{
    public async Task<CharacterSelectionCatalog> BuildAsync(
        ITravelApi api,
        int discoveryConcurrency,
        CancellationToken cancellationToken)
    {
        var discovery = await characterDiscovery.DiscoverAsync(api, discoveryConcurrency, cancellationToken);
        IReadOnlyList<ActiveTravelOrder> activeOrders = [];
        string? activeTravelOrderFailure = null;

        try
        {
            activeOrders = await api.GetActiveTravelOrdersAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            activeTravelOrderFailure = exception.Message;
        }

        return new CharacterSelectionCatalog(
            BuildSelections(discovery.Characters, activeOrders),
            discovery.Failures,
            activeTravelOrderFailure);
    }

    private static IReadOnlyList<CharacterSelection> BuildSelections(
        IReadOnlyList<Character> directCharacters,
        IReadOnlyList<ActiveTravelOrder> activeOrders)
    {
        var selections = new List<CharacterSelection>(directCharacters.Count + activeOrders.Count);
        var directRoleIds = new HashSet<string>(StringComparer.Ordinal);
        var directHomeNames = new HashSet<string>(StringComparer.Ordinal);
        var activeKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var character in directCharacters)
        {
            selections.Add(new CharacterSelection(
                character.RoleId,
                character.RoleName,
                character.SourceRegion,
                character.SourceWorld,
                character,
                null));
            directRoleIds.Add(character.RoleId);
            directHomeNames.Add(BuildHomeNameKey(character.SourceRegion, character.SourceWorld, character.RoleName));
        }

        foreach (var order in activeOrders)
        {
            if (!string.IsNullOrWhiteSpace(order.RoleId) && directRoleIds.Contains(order.RoleId))
            {
                continue;
            }

            if (directHomeNames.Contains(BuildHomeNameKey(order.HomeRegion, order.HomeWorld, order.RoleName)))
            {
                continue;
            }

            if (!activeKeys.Add(BuildActiveKey(order)))
            {
                continue;
            }

            selections.Add(new CharacterSelection(
                order.RoleId ?? string.Empty,
                order.RoleName,
                order.HomeRegion,
                order.HomeWorld,
                null,
                order));
        }

        return selections
            .OrderBy(selection => selection.HomeRegion.AreaName, StringComparer.Ordinal)
            .ThenBy(selection => selection.HomeWorld.GroupName, StringComparer.Ordinal)
            .ThenBy(selection => selection.RoleName, StringComparer.Ordinal)
            .ThenBy(selection => selection.RequiresReturnHome ? 1 : 0)
            .ToArray();
    }

    private static string BuildHomeNameKey(SourceRegion region, SourceWorld world, string roleName)
    {
        return $"{region.AreaId}:{world.GroupId}:{roleName}";
    }

    private static string BuildActiveKey(ActiveTravelOrder order)
    {
        return string.IsNullOrWhiteSpace(order.RoleId)
            ? $"home:{BuildHomeNameKey(order.HomeRegion, order.HomeWorld, order.RoleName)}"
            : $"id:{order.RoleId}";
    }
}
