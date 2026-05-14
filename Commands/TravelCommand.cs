using DCTravelCli.Services;
using Spectre.Console.Cli;

namespace DCTravelCli.Commands;

public sealed class TravelCommand(ITravelFlow travelFlow) : AsyncCommand<TravelSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        TravelSettings settings,
        CancellationToken cancellationToken)
    {
        await travelFlow.RunAsync(
            new TravelRunOptions
            {
                Session = SettingsMapper.ToSessionOptions(settings),
                AssumeYes = settings.Yes,
                DiscoveryConcurrency = settings.DiscoveryConcurrency,
                Verbose = settings.Verbose
            },
            cancellationToken);

        return 0;
    }
}
