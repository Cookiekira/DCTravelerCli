using DCTravelCli.Domain;
using DCTravelCli.Ux;
using Spectre.Console;

namespace DCTravelCli.Services;

public sealed class TravelFlow(
    ISessionAcquirer sessionAcquirer,
    ITravelApiFactory apiFactory,
    ICharacterSelectionCatalogBuilder catalogBuilder,
    IReturnHomeService returnHomeService,
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

        var catalog = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在扫描角色...", _ =>
                catalogBuilder.BuildAsync(api, options.DiscoveryConcurrency, cancellationToken));

        ReportCatalogIssues(catalog, options.Verbose);

        if (catalog.Characters.Count == 0)
        {
            throw new InvalidOperationException(BuildNoCharacterMessage(catalog));
        }

        var selection = prompts.SelectCharacter(catalog.Characters);

        if (selection.RequiresReturnHome)
        {
            await RunReturnThenTravelAsync(api, selection, options, cancellationToken);
            return;
        }

        var character = selection.DirectCharacter
            ?? throw new InvalidOperationException("角色选择缺少可提交传送的角色数据。");
        await RunDirectTravelAsync(api, character, options, cancellationToken);
    }

    private async Task RunDirectTravelAsync(
        ITravelApi api,
        Character character,
        TravelRunOptions options,
        CancellationToken cancellationToken)
    {
        var target = await SelectAvailableTargetAsync(
            api,
            character.SourceRegion,
            character.SourceWorld,
            cancellationToken);
        var selection = new TravelSelection(character, target);

        if (!prompts.ConfirmOrder(selection, options.AssumeYes))
        {
            console.MarkupLine("[yellow]已取消。[/]");
            return;
        }

        await SubmitAndTrackTravelOrderAsync(api, selection, options.Verbose, cancellationToken);
    }

    private async Task RunReturnThenTravelAsync(
        ITravelApi api,
        CharacterSelection selection,
        TravelRunOptions options,
        CancellationToken cancellationToken)
    {
        var order = selection.ActiveTravelOrder
            ?? throw new InvalidOperationException("角色选择缺少可返回的旅行订单。");
        var target = await SelectAvailableTargetAsync(
            api,
            order.HomeRegion,
            order.HomeWorld,
            cancellationToken);
        var returnThenTravel = new ReturnThenTravelSelection(order, target);

        if (!prompts.ConfirmReturnThenTravel(returnThenTravel, options.AssumeYes))
        {
            console.MarkupLine("[yellow]已取消。[/]");
            return;
        }

        await returnHomeService.ReturnHomeAsync(
            api,
            order,
            options.ReturnHome with { Verbose = options.Verbose },
            cancellationToken);

        var refreshedCharacter = await RefreshReturnedCharacterAsync(api, order, cancellationToken);
        var refreshedTarget = await RefreshSelectedTargetAsync(
            api,
            refreshedCharacter,
            target,
            cancellationToken);
        await SubmitAndTrackTravelOrderAsync(
            api,
            new TravelSelection(refreshedCharacter, refreshedTarget),
            options.Verbose,
            cancellationToken);
    }

    private async Task<AvailableTarget> SelectAvailableTargetAsync(
        ITravelApi api,
        SourceRegion sourceRegion,
        SourceWorld sourceWorld,
        CancellationToken cancellationToken)
    {
        var targetRegions = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在读取可用目标服务器...", _ =>
                api.GetTargetRegionsAsync(sourceRegion, sourceWorld, cancellationToken));

        var availableRegions = TargetAvailability.FilterAvailableTargets(
            targetRegions,
            sourceRegion.AreaId);

        if (availableRegions.Count == 0)
        {
            throw new InvalidOperationException("当前角色没有可用目标服务器。");
        }

        var targetRegion = prompts.SelectTargetRegion(availableRegions);
        var targetWorld = prompts.SelectTargetWorld(targetRegion);
        return new AvailableTarget(targetRegion, targetWorld);
    }

    private async Task<Character> RefreshReturnedCharacterAsync(
        ITravelApi api,
        ActiveTravelOrder order,
        CancellationToken cancellationToken)
    {
        var characters = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在刷新返回后的角色数据...", _ =>
                api.GetCharactersAsync(order.HomeRegion, order.HomeWorld, cancellationToken));
        var matches = MatchReturnedCharacter(order, characters);

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException("角色已返回原服，但刷新角色列表后没有找到同一个角色。请重新运行命令选择角色。"),
            _ => throw new InvalidOperationException("角色已返回原服，但刷新角色列表后匹配到多个同名角色。请重新运行命令选择角色。")
        };
    }

    private async Task<AvailableTarget> RefreshSelectedTargetAsync(
        ITravelApi api,
        Character character,
        AvailableTarget target,
        CancellationToken cancellationToken)
    {
        var targetRegions = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在重新确认目标服务器可用性...", _ =>
                api.GetTargetRegionsAsync(character.SourceRegion, character.SourceWorld, cancellationToken));
        var availableRegions = TargetAvailability.FilterAvailableTargets(
            targetRegions,
            character.SourceRegion.AreaId);
        var refreshedRegion = availableRegions.FirstOrDefault(region => region.AreaId == target.Region.AreaId);
        var refreshedWorld = refreshedRegion?.Worlds.FirstOrDefault(world => world.GroupId == target.World.GroupId);

        if (refreshedRegion is null || refreshedWorld is null)
        {
            throw new InvalidOperationException("角色已返回原服，但目标服务器当前不可用，未提交新的超域传送订单。");
        }

        return new AvailableTarget(refreshedRegion, refreshedWorld);
    }

    private async Task SubmitAndTrackTravelOrderAsync(
        ITravelApi api,
        TravelSelection selection,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var order = await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在提交订单...", _ => api.SubmitTravelOrderAsync(selection, cancellationToken));

        console.MarkupLine($"订单已提交：[green]{Markup.Escape(order.OrderId)}[/]");
        await TrackOrderAsync(api, order, verbose, cancellationToken);
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

    private static IReadOnlyList<Character> MatchReturnedCharacter(
        ActiveTravelOrder order,
        IReadOnlyList<Character> characters)
    {
        if (!string.IsNullOrWhiteSpace(order.RoleId))
        {
            return characters
                .Where(character => string.Equals(character.RoleId, order.RoleId, StringComparison.Ordinal))
                .ToArray();
        }

        return characters
            .Where(character => string.Equals(character.RoleName, order.RoleName, StringComparison.Ordinal))
            .ToArray();
    }

    private void ReportCatalogIssues(CharacterSelectionCatalog catalog, bool verbose)
    {
        if (!string.IsNullOrWhiteSpace(catalog.ActiveTravelOrderFailure))
        {
            console.MarkupLine(verbose
                ? $"[yellow]旅行中订单读取失败：{Markup.Escape(catalog.ActiveTravelOrderFailure)}[/]"
                : "[yellow]旅行中订单读取失败，仅显示当前可直接传送的角色。使用 --verbose 查看详情。[/]");
        }

        if (catalog.DiscoveryFailures.Count == 0)
        {
            return;
        }

        if (!verbose && catalog.Characters.Count > 0)
        {
            console.MarkupLine($"[yellow]有 {catalog.DiscoveryFailures.Count} 个服务器扫描失败，已继续显示可用角色。使用 --verbose 查看详情。[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Yellow)
            .AddColumn("大区")
            .AddColumn("服务器")
            .AddColumn("原因");

        foreach (var failure in catalog.DiscoveryFailures)
        {
            table.AddRow(
                Markup.Escape(failure.Region.AreaName),
                Markup.Escape(failure.World.GroupName),
                Markup.Escape(failure.Message));
        }

        console.Write(table);
    }

    private static string BuildNoCharacterMessage(CharacterSelectionCatalog catalog)
    {
        if (catalog.DiscoveryFailures.Count == 0 && string.IsNullOrWhiteSpace(catalog.ActiveTravelOrderFailure))
        {
            return "没有找到可传送或可返回的角色。";
        }

        return $"没有找到可传送或可返回的角色，且有 {catalog.DiscoveryFailures.Count} 个服务器扫描失败。请使用 --verbose 查看详情。";
    }
}
