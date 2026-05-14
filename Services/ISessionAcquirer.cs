namespace DCTravelerCli.Services;

public interface ISessionAcquirer
{
    Task<OfficialSession> AcquireAsync(
        SessionAcquisitionOptions options,
        CancellationToken cancellationToken);
}
