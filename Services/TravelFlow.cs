using DCTravelCli.Domain;
using DCTravelCli.Ux;
using Spectre.Console;

namespace DCTravelCli.Services;

public sealed class TravelFlow(
    ISessionAcquirer sessionAcquirer,
    ITravelApiFactory apiFactory,
    ICharacterDiscovery characterDiscovery,
    ITravelPrompts prompts,
    IAnsiConsole console) : ITravelFlow
{
    public async Task RunAsync(TravelRunOptions options, CancellationToken cancellationToken)
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

        var discovery = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在扫描角色...", _ =>
                characterDiscovery.DiscoverAsync(api, options.DiscoveryConcurrency, cancellationToken));

        ReportDiscoveryFailures(discovery, options.Verbose);

        if (discovery.Characters.Count == 0)
        {
            throw new InvalidOperationException(BuildNoCharacterMessage(discovery.Failures));
        }

        var character = prompts.SelectCharacter(discovery.Characters);

        var targetRegions = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在读取可用目标服务器...", _ =>
                api.GetTargetRegionsAsync(character, cancellationToken));

        var availableRegions = TargetAvailability.FilterAvailableTargets(
            targetRegions,
            character.SourceRegion.AreaId);

        if (availableRegions.Count == 0)
        {
            throw new InvalidOperationException("当前角色没有可用目标服务器。");
        }

        var targetRegion = prompts.SelectTargetRegion(availableRegions);
        var targetWorld = prompts.SelectTargetWorld(targetRegion);
        var selection = new TravelSelection(character, new AvailableTarget(targetRegion, targetWorld));

        if (!prompts.ConfirmOrder(selection, options.AssumeYes))
        {
            console.MarkupLine("[yellow]已取消。[/]");
            return;
        }

        var order = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在提交订单...", _ => api.SubmitTravelOrderAsync(selection, cancellationToken));

        console.MarkupLine($"订单已提交：[green]{Markup.Escape(order.OrderId)}[/]");
        await TrackOrderAsync(api, order, options.Verbose, cancellationToken);
    }

    private async Task TrackOrderAsync(
        ITravelApi api,
        TravelOrder order,
        bool verbose,
        CancellationToken cancellationToken)
    {
        MigrationStatus? previousStatus = null;

        while (true)
        {
            var snapshot = await console.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("等待订单状态...", async context =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                    var status = await api.GetOrderStatusAsync(order, cancellationToken);
                    context.Status(MigrationStatusText.Format(status.Status));
                    return status;
                });

            if (verbose || previousStatus != snapshot.Status)
            {
                OrderTrackingOutput.WriteStatusChange(console, DateTimeOffset.Now, snapshot.Status);
            }

            previousStatus = snapshot.Status;

            switch (snapshot.Status)
            {
                case MigrationStatus.Completed:
                    console.MarkupLine("[green]超域传送已完成。请重新登录游戏。[/]");
                    return;

                case MigrationStatus.PreCheckFailed:
                case MigrationStatus.TeleportFailed:
                    throw new InvalidOperationException($"传送失败：{snapshot.Message ?? "官网没有返回失败原因。"}");

                case MigrationStatus.NeedConfirm:
                    var confirm = prompts.ConfirmOfficialSecondStep();
                    await api.ConfirmOrderAsync(order, confirm, cancellationToken);
                    if (!confirm)
                    {
                        console.MarkupLine("[yellow]已放弃本次传送。[/]");
                        return;
                    }

                    console.MarkupLine("[green]已确认，继续等待完成。[/]");
                    break;
            }
        }
    }

    private void ReportDiscoveryFailures(CharacterDiscoveryResult discovery, bool verbose)
    {
        if (discovery.Failures.Count == 0)
        {
            return;
        }

        if (!verbose && discovery.Characters.Count > 0)
        {
            console.MarkupLine($"[yellow]有 {discovery.Failures.Count} 个服务器扫描失败，已继续显示可用角色。使用 --verbose 查看详情。[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Yellow)
            .AddColumn("大区")
            .AddColumn("服务器")
            .AddColumn("原因");

        foreach (var failure in discovery.Failures)
        {
            table.AddRow(
                Markup.Escape(failure.Region.AreaName),
                Markup.Escape(failure.World.GroupName),
                Markup.Escape(failure.Message));
        }

        console.Write(table);
    }

    private static string BuildNoCharacterMessage(IReadOnlyList<DiscoveryFailure> failures)
    {
        if (failures.Count == 0)
        {
            return "没有找到可传送角色。";
        }

        return $"没有找到可传送角色，且有 {failures.Count} 个服务器扫描失败。请使用 --verbose 查看详情。";
    }
}
