using DCTravelCli.Domain;

namespace DCTravelCli.Services;

public interface ICharacterSelectionCatalogBuilder
{
    Task<CharacterSelectionCatalog> BuildAsync(
        ITravelApi api,
        int discoveryConcurrency,
        CancellationToken cancellationToken);
}
