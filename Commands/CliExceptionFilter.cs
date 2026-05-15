using ConsoleAppFramework;
using Spectre.Console;

namespace DCTravelerCli.Commands;

internal sealed class CliExceptionFilter(
    IAnsiConsole console,
    ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(
        ConsoleAppContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Environment.ExitCode = 1;
            console.MarkupLine("[yellow]已取消，正在退出。[/]");
        }
        catch (Exception exception)
        {
            Environment.ExitCode = 1;
            console.MarkupLine($"[red]错误：[/] {Markup.Escape(exception.Message)}");
        }
    }
}
