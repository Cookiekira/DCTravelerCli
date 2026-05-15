using DCTravelerCli.Ux;
using Spectre.Console;

namespace DCTravelerCli.Services;

public sealed class ReturnFlow(
    ISessionAcquirer sessionAcquirer,
    ITravelApiFactory apiFactory,
    IReturnHomeService returnHomeService,
    ITravelPrompts prompts,
    IAnsiConsole console) : IReturnFlow
{
    public async Task RunAsync(ReturnRunOptions options, CancellationToken cancellationToken)
    {
        console.MarkupLine("[grey]准备登录会话...[/]");
        var session = await sessionAcquirer.AcquireAsync(options.Session, cancellationToken);

        if (!string.IsNullOrWhiteSpace(session.DisplayAccount))
        {
            console.MarkupLine($"已登录：[green]{Markup.Escape(session.DisplayAccount)}[/]");
        }
        else
        {
            console.MarkupLine("[green]已获取登录会话。[/]");
        }

        using var api = apiFactory.Create(session);
        var orders = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在读取旅行中角色...", _ => api.GetActiveTravelOrdersAsync(cancellationToken));

        if (orders.Count == 0)
        {
            throw new InvalidOperationException("没有找到可返回原服的旅行中角色。");
        }

        var order = prompts.SelectReturnOrder(orders);
        if (!prompts.ConfirmReturn(order, options.AssumeYes))
        {
            console.MarkupLine("[yellow]已取消。[/]");
            return;
        }

        var result = await returnHomeService.ReturnHomeAsync(
            api,
            order,
            options.ReturnHome with { Verbose = options.Verbose },
            cancellationToken);
        result.ThrowIfNotConfirmed();

        console.MarkupLine($"[green]角色 {Markup.Escape(order.RoleName)} 已返回原服。[/]");
    }
}
