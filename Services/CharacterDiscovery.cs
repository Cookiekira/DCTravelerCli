using System.Collections.Concurrent;
using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public sealed class CharacterDiscovery : ICharacterDiscovery
{
    public async Task<CharacterDiscoveryResult> DiscoverAsync(
        ITravelApi api,
        int concurrency,
        CancellationToken cancellationToken)
    {
        var sourceRegions = await api.GetSourceRegionsAsync(cancellationToken);
        var workItems = sourceRegions
            .SelectMany(region => region.Worlds.Select(world => (Region: region, World: world)))
            .ToArray();

        var characters = new ConcurrentBag<Character>();
        var failures = new ConcurrentBag<DiscoveryFailure>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, concurrency),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(workItems, options, async (item, token) =>
        {
            try
            {
                var found = await api.GetCharactersAsync(item.Region, item.World, token);
                foreach (var character in found)
                {
                    characters.Add(character);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures.Add(new DiscoveryFailure(item.Region, item.World, exception.Message));
            }
        });

        return new CharacterDiscoveryResult(
            characters
                .OrderBy(character => character.SourceRegion.AreaName, StringComparer.Ordinal)
                .ThenBy(character => character.SourceWorld.GroupName, StringComparer.Ordinal)
                .ThenBy(character => character.RoleName, StringComparer.Ordinal)
                .ToArray(),
            failures
                .OrderBy(failure => failure.Region.AreaName, StringComparer.Ordinal)
                .ThenBy(failure => failure.World.GroupName, StringComparer.Ordinal)
                .ToArray());
    }
}
