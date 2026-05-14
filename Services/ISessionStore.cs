namespace DCTravelCli.Services;

public interface ISessionStore
{
    Task<OfficialSession?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(OfficialSession session, CancellationToken cancellationToken);

    Task DeleteAsync(CancellationToken cancellationToken);
}
