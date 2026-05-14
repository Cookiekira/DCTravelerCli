using DCTravelCli.Domain;
using DCTravelCli.Services;
using Spectre.Console.Testing;

namespace DCTravelCli.Tests;

public sealed class ReturnHomeServiceTests
{
    [Fact]
    public async Task ReturnHomeAsync_submits_and_confirms_return_order_success()
    {
        var order = CreateActiveOrder();
        var api = new FakeTravelApi(order)
        {
            MigrationOrderResponses =
            [
                [new MigrationOrderSummary("R-1", 5, 5, 0, "返回成功")]
            ]
        };

        var result = await new ReturnHomeService(new TestConsole()).ReturnHomeAsync(
            api,
            order,
            FastOptions(),
            CancellationToken.None);

        Assert.Equal("R-1", result.ReturnOrder.OrderId);
        Assert.Equal(1, api.SubmitCount);
    }

    [Fact]
    public async Task ReturnHomeAsync_retries_when_status_is_not_confirmed()
    {
        var order = CreateActiveOrder();
        var api = new FakeTravelApi(order)
        {
            MigrationOrderResponses =
            [
                [new MigrationOrderSummary("T-1", 4, 5, 1, "旅行中")],
                [new MigrationOrderSummary("T-1", 4, 5, 3, "旅行结束")]
            ]
        };

        await new ReturnHomeService(new TestConsole()).ReturnHomeAsync(
            api,
            order,
            FastOptions() with { MaxAttempts = 2 },
            CancellationToken.None);

        Assert.Equal(2, api.SubmitCount);
    }

    [Fact]
    public async Task ReturnHomeAsync_fails_when_current_world_cannot_be_matched()
    {
        var order = CreateActiveOrder();
        var api = new FakeTravelApi(order)
        {
            ReturnRegions = [new SourceRegion(3, "猫小胖", [new SourceWorld(30, "C", "紫水栈桥")])]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ReturnHomeService(new TestConsole()).ReturnHomeAsync(
                api,
                order,
                FastOptions(),
                CancellationToken.None));

        Assert.Contains("无法在官网返回区服列表中匹配当前大区", exception.Message);
        Assert.Equal(0, api.SubmitCount);
    }

    private static ReturnHomeOptions FastOptions()
    {
        return new ReturnHomeOptions
        {
            MaxAttempts = 1,
            RetryDelay = TimeSpan.Zero,
            StatusPollAttempts = 1,
            StatusPollInterval = TimeSpan.Zero
        };
    }

    private static ActiveTravelOrder CreateActiveOrder()
    {
        return new ActiveTravelOrder(
            "T-1",
            "100",
            "Cookie",
            new SourceRegion(1, "陆行鸟", []),
            new SourceWorld(10, "A", "红玉海"),
            new SourceRegion(2, "莫古力", []),
            new SourceWorld(20, "B", "神意之地"),
            "旅行中");
    }

    private sealed class FakeTravelApi(ActiveTravelOrder order) : ITravelApi
    {
        private int migrationOrderReadCount;

        public int SubmitCount { get; private set; }

        public IReadOnlyList<SourceRegion> ReturnRegions { get; init; } =
        [
            new SourceRegion(
                order.CurrentRegion.AreaId,
                order.CurrentRegion.AreaName,
                [order.CurrentWorld])
        ];

        public IReadOnlyList<IReadOnlyList<MigrationOrderSummary>> MigrationOrderResponses { get; init; } = [];

        public Task<LoginProbe> ProbeLoginAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<SourceRegion>> GetSourceRegionsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Character>> GetCharactersAsync(
            SourceRegion sourceRegion,
            SourceWorld sourceWorld,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<TargetRegion>> GetTargetRegionsAsync(
            SourceRegion sourceRegion,
            SourceWorld sourceWorld,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<MigrationOrderSummary>> GetMigrationOrdersAsync(CancellationToken cancellationToken)
        {
            var index = Math.Min(migrationOrderReadCount, MigrationOrderResponses.Count - 1);
            migrationOrderReadCount++;
            return Task.FromResult(MigrationOrderResponses[index]);
        }

        public Task<IReadOnlyList<ActiveTravelOrder>> GetActiveTravelOrdersAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<SourceRegion>> GetReturnSourceRegionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ReturnRegions);
        }

        public Task<ReturnHomeOrder> SubmitReturnHomeAsync(
            ReturnHomeSelection selection,
            CancellationToken cancellationToken)
        {
            SubmitCount++;
            return Task.FromResult(new ReturnHomeOrder("R-1", "ok"));
        }

        public Task<TravelOrder> SubmitTravelOrderAsync(
            TravelSelection selection,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<OrderStatusSnapshot> GetOrderStatusAsync(
            TravelOrder order,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ConfirmOrderAsync(
            TravelOrder order,
            bool confirm,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
