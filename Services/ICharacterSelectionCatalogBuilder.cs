using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public interface ICharacterSelectionCatalogBuilder
{
    Task<CharacterSelectionCatalog> BuildAsync(
        ICharacterSelectionCatalogApi api,
        int discoveryConcurrency,
        CancellationToken cancellationToken);
}
