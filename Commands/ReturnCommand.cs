using DCTravelCli.Services;
using Spectre.Console.Cli;

namespace DCTravelCli.Commands;

public sealed class ReturnCommand(IReturnFlow returnFlow) : AsyncCommand<ReturnSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        ReturnSettings settings,
        CancellationToken cancellationToken)
    {
        await returnFlow.RunAsync(
            new ReturnRunOptions
            {
                Session = SettingsMapper.ToSessionOptions(settings),
                AssumeYes = settings.Yes,
                Verbose = settings.Verbose,
                ReturnHome = new ReturnHomeOptions
                {
                    Verbose = settings.Verbose
                }
            },
            cancellationToken);

        return 0;
    }
}
