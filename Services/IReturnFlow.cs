namespace DCTravelCli.Services;

public interface IReturnFlow
{
    Task RunAsync(ReturnRunOptions options, CancellationToken cancellationToken);
}
