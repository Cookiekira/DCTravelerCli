using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public interface IReturnHomeService
{
    Task<ReturnHomeResult> ReturnHomeAsync(
        ITravelApi api,
        ActiveTravelOrder order,
        ReturnHomeOptions options,
        CancellationToken cancellationToken);
}
