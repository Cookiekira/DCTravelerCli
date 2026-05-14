using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public interface ICharacterDiscovery
{
    Task<CharacterDiscoveryResult> DiscoverAsync(
        ITravelApi api,
        int concurrency,
        CancellationToken cancellationToken);
}
