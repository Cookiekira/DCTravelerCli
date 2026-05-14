using DCTravelerCli.Domain;
using Spectre.Console;

namespace DCTravelerCli.Services;

public sealed class ReturnHomeService(IAnsiConsole console) : IReturnHomeService
{
    public async Task<ReturnHomeResult> ReturnHomeAsync(
        ITravelApi api,
        ActiveTravelOrder order,
        ReturnHomeOptions options,
        CancellationToken cancellationToken)
    {
        var selection = await ResolveReturnHomeSelectionAsync(api, order, cancellationToken);
        Exception? lastException = null;
        ReturnHomeOrder? lastReturnOrder = null;

        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            if (attempt > 1)
            {
                console.MarkupLine($"[yellow]等待 {options.RetryDelay.TotalSeconds:0} 秒后重试超域返回...[/]");
                await Task.Delay(options.RetryDelay, cancellationToken);
            }

            try
            {
                console.MarkupLine($"[grey]提交超域返回请求（第 {attempt}/{options.MaxAttempts} 次）...[/]");
                lastReturnOrder = await api.SubmitReturnHomeAsync(selection, cancellationToken);

                if (await PollReturnCompleteAsync(
                    api,
                    order.OrderId,
                    lastReturnOrder.OrderId,
                    options,
                    cancellationToken))
                {
                    return new ReturnHomeResult(order, lastReturnOrder);
                }

                console.MarkupLine("[yellow]本次返回状态未确认，将重试。[/]");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastException = exception;
                console.MarkupLine($"[yellow]本次返回失败：{Markup.Escape(exception.Message)}[/]");
            }
        }

        var message = lastException is not null
            ? lastException.Message
            : "返回状态未在限定时间内确认。";
        throw new InvalidOperationException($"超域返回失败，已尝试 {options.MaxAttempts} 次：{message}");
    }

    private static async Task<ReturnHomeSelection> ResolveReturnHomeSelectionAsync(
        ITravelApi api,
        ActiveTravelOrder order,
        CancellationToken cancellationToken)
    {
        var regions = await api.GetReturnSourceRegionsAsync(cancellationToken);
        var currentRegion = regions.FirstOrDefault(region =>
            region.AreaId == order.CurrentRegion.AreaId ||
            string.Equals(region.AreaName, order.CurrentRegion.AreaName, StringComparison.Ordinal));

        if (currentRegion is null)
        {
            throw new InvalidOperationException($"无法在官网返回区服列表中匹配当前大区：{order.CurrentRegion.AreaName}。");
        }

        var currentWorld = currentRegion.Worlds.FirstOrDefault(world =>
            world.GroupId == order.CurrentWorld.GroupId ||
            string.Equals(world.GroupName, order.CurrentWorld.GroupName, StringComparison.Ordinal));

        if (currentWorld is null)
        {
            throw new InvalidOperationException($"无法在官网返回区服列表中匹配当前服务器：{order.CurrentWorld.GroupName}。");
        }

        return new ReturnHomeSelection(order, currentRegion, currentWorld);
    }

    private async Task<bool> PollReturnCompleteAsync(
        ITravelApi api,
        string travelOrderId,
        string? returnOrderId,
        ReturnHomeOptions options,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= options.StatusPollAttempts; attempt++)
        {
            var orders = await api.GetMigrationOrdersAsync(cancellationToken);

            if (IsReturnConfirmed(orders, travelOrderId, returnOrderId))
            {
                console.MarkupLine("[green]超域返回已完成。[/]");
                return true;
            }

            if (IsReturnFailed(orders, travelOrderId, returnOrderId))
            {
                console.MarkupLine("[yellow]官网订单显示本次返回失败。[/]");
                return false;
            }

            if (options.Verbose)
            {
                console.MarkupLine($"[grey]返回状态轮询 {attempt}/{options.StatusPollAttempts}：尚未完成。[/]");
            }

            if (attempt < options.StatusPollAttempts)
            {
                await Task.Delay(options.StatusPollInterval, cancellationToken);
            }
        }

        return false;
    }

    private static bool IsReturnConfirmed(
        IReadOnlyList<MigrationOrderSummary> orders,
        string travelOrderId,
        string? returnOrderId)
    {
        return orders.Any(order =>
                order.MigrationType == OfficialEndpoints.ReturnMigrationType &&
                MatchesReturnOrder(order, travelOrderId, returnOrderId) &&
                Contains(order.StatusDescription, "返回成功")) ||
            orders.Any(order =>
                order.OrderId == travelOrderId &&
                order.MigrationType == OfficialEndpoints.MigrationType &&
                (order.TravelStatus == 3 || Contains(order.StatusDescription, "旅行结束")));
    }

    private static bool IsReturnFailed(
        IReadOnlyList<MigrationOrderSummary> orders,
        string travelOrderId,
        string? returnOrderId)
    {
        return orders.Any(order =>
            order.MigrationType == OfficialEndpoints.ReturnMigrationType &&
            MatchesReturnOrder(order, travelOrderId, returnOrderId) &&
            Contains(order.StatusDescription, "失败"));
    }

    private static bool MatchesReturnOrder(
        MigrationOrderSummary order,
        string travelOrderId,
        string? returnOrderId)
    {
        return order.OrderId == returnOrderId || order.OrderId == travelOrderId;
    }

    private static bool Contains(string? value, string expected)
    {
        return value?.Contains(expected, StringComparison.Ordinal) == true;
    }
}
