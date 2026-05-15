using System.Net;
using DCTravelerCli.Domain;
using DCTravelerCli.Services;
using DCTravelerCli.Ux;
using Spectre.Console.Testing;

namespace DCTravelerCli.Tests;

public sealed class TravelFlowTests
{
    private readonly SourceRegion homeRegion = new(1, "陆行鸟", []);
    private readonly SourceWorld homeWorld = new(10, "A", "红玉海");
    private readonly TargetRegion targetRegion = new(2, "莫古力", []);
    private readonly TargetWorld targetWorld = new(20, "B", "神意之地");

    [Fact]
    public async Task RunAsync_submits_one_direct_travel_order_after_confirmation()
    {
        var character = new Character("100", "Cookie", homeRegion, homeWorld);
        var api = new FakeTravelApi
        {
            TargetRegions =
            [
                targetRegion with { Worlds = [targetWorld] }
            ],
            OrderStatuses =
            [
                new OrderStatusSnapshot(MigrationStatus.Completed, null)
            ]
        };
        var prompts = new FakeTravelPrompts
        {
            SelectedCharacter = new CharacterSelection(
                character.RoleId,
                character.RoleName,
                character.SourceRegion,
                character.SourceWorld,
                character,
                null),
            SelectedTargetRegion = targetRegion with { Worlds = [targetWorld] },
            SelectedTargetWorld = targetWorld
        };

        await CreateFlow(api, prompts, new FakeReturnHomeService(), [prompts.SelectedCharacter])
            .RunAsync(FastOptions(), CancellationToken.None);

        var submitted = Assert.Single(api.SubmittedTravelOrders);
        Assert.Same(character, submitted.Character);
        Assert.Equal(targetWorld.GroupId, submitted.Target.World.GroupId);
        Assert.True(prompts.OrderConfirmed);
        Assert.Equal(1, api.OrderStatusReadCount);
    }

    [Fact]
    public async Task RunAsync_returns_home_and_stops_when_refreshed_target_disappears()
    {
        var currentRegion = new SourceRegion(3, "猫小胖", []);
        var currentWorld = new SourceWorld(30, "C", "紫水栈桥");
        var activeOrder = new ActiveTravelOrder(
            "T-1",
            "100",
            "Cookie",
            homeRegion,
            homeWorld,
            currentRegion,
            currentWorld,
            "旅行中");
        var returnedCharacter = new Character("100", "Cookie", homeRegion, homeWorld);
        var initialTargetRegion = targetRegion with { Worlds = [targetWorld] };
        var api = new FakeTravelApi
        {
            TargetRegionResponses =
            [
                [initialTargetRegion],
                []
            ],
            CharactersAfterReturn = [returnedCharacter]
        };
        var prompts = new FakeTravelPrompts
        {
            SelectedCharacter = new CharacterSelection(
                activeOrder.RoleId ?? string.Empty,
                activeOrder.RoleName,
                activeOrder.HomeRegion,
                activeOrder.HomeWorld,
                null,
                activeOrder),
            SelectedTargetRegion = initialTargetRegion,
            SelectedTargetWorld = targetWorld
        };
        var returnHome = new FakeReturnHomeService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateFlow(api, prompts, returnHome, [prompts.SelectedCharacter])
                .RunAsync(FastOptions(), CancellationToken.None));

        Assert.Contains("目标服务器当前不可用", exception.Message);
        Assert.Single(returnHome.ReturnedOrders);
        Assert.Empty(api.SubmittedTravelOrders);
    }

    private TravelFlow CreateFlow(
        FakeTravelApi api,
        FakeTravelPrompts prompts,
        IReturnHomeService returnHomeService,
        IReadOnlyList<CharacterSelection> characters)
    {
        return new TravelFlow(
            new FakeSessionAcquirer(),
            new FakeTravelApiFactory(api),
            new FakeCatalogBuilder(characters),
            returnHomeService,
            prompts,
            new TestConsole());
    }

    private static TravelRunOptions FastOptions()
    {
        return new TravelRunOptions
        {
            Session = new SessionAcquisitionOptions { ProfileDirectory = "profile" },
            AssumeYes = true,
            OrderTrackingPollInterval = TimeSpan.Zero,
            ReturnHome = new ReturnHomeOptions
            {
                RetryDelay = TimeSpan.Zero,
                StatusPollInterval = TimeSpan.Zero
            }
        };
    }

    private sealed class FakeSessionAcquirer : ISessionAcquirer
    {
        public Task<OfficialSession> AcquireAsync(
            SessionAcquisitionOptions options,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new OfficialSession(new CookieContainer(), "test", []));
        }
    }

    private sealed class FakeTravelApiFactory(FakeTravelApi api) : ITravelApiFactory
    {
        public ITravelApi Create(OfficialSession session)
        {
            return api;
        }
    }

    private sealed class FakeCatalogBuilder(IReadOnlyList<CharacterSelection> characters)
        : ICharacterSelectionCatalogBuilder
    {
        public Task<CharacterSelectionCatalog> BuildAsync(
            ICharacterSelectionCatalogApi api,
            int discoveryConcurrency,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CharacterSelectionCatalog(characters, [], null));
        }
    }

    private sealed class FakeReturnHomeService : IReturnHomeService
    {
        public List<ActiveTravelOrder> ReturnedOrders { get; } = [];

        public Task<ReturnHomeResult> ReturnHomeAsync(
            IReturnHomeApi api,
            ActiveTravelOrder order,
            ReturnHomeOptions options,
            CancellationToken cancellationToken)
        {
            ReturnedOrders.Add(order);
            return Task.FromResult(new ReturnHomeResult(order, new ReturnHomeOrder("R-1", "ok")));
        }
    }

    private sealed class FakeTravelPrompts : ITravelPrompts
    {
        public required CharacterSelection SelectedCharacter { get; init; }

        public required TargetRegion SelectedTargetRegion { get; init; }

        public required TargetWorld SelectedTargetWorld { get; init; }

        public bool OrderConfirmed { get; private set; }

        public CharacterSelection SelectCharacter(IReadOnlyList<CharacterSelection> characters)
        {
            return SelectedCharacter;
        }

        public ActiveTravelOrder SelectReturnOrder(IReadOnlyList<ActiveTravelOrder> orders)
        {
            return orders[0];
        }

        public TargetRegion SelectTargetRegion(IReadOnlyList<TargetRegion> regions)
        {
            return SelectedTargetRegion;
        }

        public TargetWorld SelectTargetWorld(TargetRegion region)
        {
            return SelectedTargetWorld;
        }

        public bool ConfirmOrder(TravelSelection selection, bool assumeYes)
        {
            OrderConfirmed = true;
            return true;
        }

        public bool ConfirmReturn(ActiveTravelOrder order, bool assumeYes)
        {
            return true;
        }

        public bool ConfirmReturnThenTravel(ReturnThenTravelSelection selection, bool assumeYes)
        {
            return true;
        }

        public bool ConfirmOfficialSecondStep()
        {
            return true;
        }
    }

    private sealed class FakeTravelApi : ITravelApi
    {
        private int targetRegionReadCount;

        public IReadOnlyList<TargetRegion> TargetRegions { get; init; } = [];

        public IReadOnlyList<IReadOnlyList<TargetRegion>> TargetRegionResponses { get; init; } = [];

        public IReadOnlyList<Character> CharactersAfterReturn { get; init; } = [];

        public IReadOnlyList<OrderStatusSnapshot> OrderStatuses { get; init; } = [];

        public List<TravelSelection> SubmittedTravelOrders { get; } = [];

        public int OrderStatusReadCount { get; private set; }

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
            return Task.FromResult(CharactersAfterReturn);
        }

        public Task<IReadOnlyList<TargetRegion>> GetTargetRegionsAsync(
            SourceRegion sourceRegion,
            SourceWorld sourceWorld,
            CancellationToken cancellationToken)
        {
            if (TargetRegionResponses.Count == 0)
            {
                return Task.FromResult(TargetRegions);
            }

            var index = Math.Min(targetRegionReadCount, TargetRegionResponses.Count - 1);
            targetRegionReadCount++;
            return Task.FromResult(TargetRegionResponses[index]);
        }

        public Task<IReadOnlyList<MigrationOrderSummary>> GetMigrationOrdersAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<ActiveTravelOrder>> GetActiveTravelOrdersAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<SourceRegion>> GetReturnSourceRegionsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ReturnHomeOrder> SubmitReturnHomeAsync(
            ReturnHomeSelection selection,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TravelOrder> SubmitTravelOrderAsync(
            TravelSelection selection,
            CancellationToken cancellationToken)
        {
            SubmittedTravelOrders.Add(selection);
            return Task.FromResult(new TravelOrder("O-1"));
        }

        public Task<OrderStatusSnapshot> GetOrderStatusAsync(
            TravelOrder order,
            CancellationToken cancellationToken)
        {
            var index = Math.Min(OrderStatusReadCount, OrderStatuses.Count - 1);
            OrderStatusReadCount++;
            return Task.FromResult(OrderStatuses[index]);
        }

        public Task ConfirmOrderAsync(
            TravelOrder order,
            bool confirm,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
