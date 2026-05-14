using DCTravelCli.Domain;

namespace DCTravelCli.Services;

public interface IReturnHomeService
{
    Task<ReturnHomeResult> ReturnHomeAsync(
        ITravelApi api,
        ActiveTravelOrder order,
        ReturnHomeOptions options,
        CancellationToken cancellationToken);
}
