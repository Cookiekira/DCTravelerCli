namespace DCTravelCli.Services;

public interface ITravelFlow
{
    Task RunAsync(TravelRunOptions options, CancellationToken cancellationToken);
}
