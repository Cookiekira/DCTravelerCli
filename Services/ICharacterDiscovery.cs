using DCTravelCli.Domain;

namespace DCTravelCli.Services;

public interface ICharacterDiscovery
{
    Task<CharacterDiscoveryResult> DiscoverAsync(
        ITravelApi api,
        int concurrency,
        CancellationToken cancellationToken);
}
