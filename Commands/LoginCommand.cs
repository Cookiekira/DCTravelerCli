using DCTravelCli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DCTravelCli.Commands;

public sealed class LoginCommand(
    ISessionAcquirer sessionAcquirer,
    IAnsiConsole console) : AsyncCommand<SessionSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        SessionSettings settings,
        CancellationToken cancellationToken)
    {
        var session = await sessionAcquirer.AcquireAsync(
            SettingsMapper.ToSessionOptions(settings, forceRefresh: true),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(session.DisplayAccount))
        {
            console.MarkupLine($"登录会话已刷新：[green]{Markup.Escape(session.DisplayAccount)}[/]");
        }
        else
        {
            console.MarkupLine("[green]登录会话已刷新。[/]");
        }

        return 0;
    }
}
