using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public interface ICharacterDiscovery
{
    Task<CharacterDiscoveryResult> DiscoverAsync(
        ICharacterDiscoveryApi api,
        int concurrency,
        CancellationToken cancellationToken);
}
