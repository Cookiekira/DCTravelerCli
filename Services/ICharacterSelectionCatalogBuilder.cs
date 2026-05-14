using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public interface ICharacterSelectionCatalogBuilder
{
    Task<CharacterSelectionCatalog> BuildAsync(
        ITravelApi api,
        int discoveryConcurrency,
        CancellationToken cancellationToken);
}
