using System.Text.Json.Nodes;
using DCTravelCli.Domain;
using DCTravelCli.Services;

namespace DCTravelCli.Tests;

public sealed class CharacterSelectionCatalogBuilderTests
{
    [Fact]
    public async Task BuildAsync_includes_away_from_home_orders()
    {
        var homeRegion = new SourceRegion(1, "陆行鸟", []);
        var homeWorld = new SourceWorld(10, "A", "红玉海");
        var currentRegion = new SourceRegion(2, "莫古力", []);
        var currentWorld = new SourceWorld(20, "B", "神意之地");
        var api = new FakeTravelApi
        {
            ActiveTravelOrders =
            [
                new ActiveTravelOrder(
                    "T-1",
                    "100",
                    "Cookie",
                    homeRegion,
                    homeWorld,
                    currentRegion,
                    currentWorld,
                    "旅行中")
            ]
        };

        var catalog = await new CharacterSelectionCatalogBuilder(new FakeCharacterDiscovery())
            .BuildAsync(api, 4, CancellationToken.None);

        Assert.Single(catalog.Characters);
        Assert.True(catalog.Characters[0].RequiresReturnHome);
        Assert.Equal("神意之地", catalog.Characters[0].CurrentWorld.GroupName);
    }

    [Fact]
    public async Task BuildAsync_prefers_direct_character_when_active_order_is_duplicate()
    {
        var homeRegion = new SourceRegion(1, "陆行鸟", []);
        var homeWorld = new SourceWorld(10, "A", "红玉海");
        var directCharacter = new Character(
            "100",
            "Cookie",
            homeRegion,
            homeWorld,
            new JsonObject { ["roleId"] = "100", ["roleName"] = "Cookie" });
        var api = new FakeTravelApi
        {
            ActiveTravelOrders =
            [
                new ActiveTravelOrder(
                    "T-1",
                    "100",
                    "Cookie",
                    homeRegion,
                    homeWorld,
                    new SourceRegion(2, "莫古力", []),
                    new SourceWorld(20, "B", "神意之地"),
                    "旅行中")
            ]
        };

        var catalog = await new CharacterSelectionCatalogBuilder(new FakeCharacterDiscovery([directCharacter]))
            .BuildAsync(api, 4, CancellationToken.None);

        Assert.Single(catalog.Characters);
        Assert.False(catalog.Characters[0].RequiresReturnHome);
        Assert.Same(directCharacter, catalog.Characters[0].DirectCharacter);
    }

    [Fact]
    public async Task BuildAsync_collapses_duplicate_active_orders_for_same_role()
    {
        var homeRegion = new SourceRegion(1, "莫古力", []);
        var homeWorld = new SourceWorld(10, "A", "白银乡");
        var api = new FakeTravelApi
        {
            ActiveTravelOrders =
            [
                new ActiveTravelOrder(
                    "new",
                    "100",
                    "斜膀泰迪",
                    homeRegion,
                    homeWorld,
                    new SourceRegion(2, "陆行鸟", []),
                    new SourceWorld(22, "new", "红玉海"),
                    "旅行中"),
                new ActiveTravelOrder(
                    "old",
                    "100",
                    "斜膀泰迪",
                    homeRegion,
                    homeWorld,
                    new SourceRegion(2, "陆行鸟", []),
                    new SourceWorld(21, "old", "萌芽池"),
                    "旅行中")
            ]
        };

        var catalog = await new CharacterSelectionCatalogBuilder(new FakeCharacterDiscovery())
            .BuildAsync(api, 4, CancellationToken.None);

        Assert.Single(catalog.Characters);
        Assert.Equal("红玉海", catalog.Characters[0].CurrentWorld.GroupName);
    }

    private sealed class FakeCharacterDiscovery(IReadOnlyList<Character>? characters = null) : ICharacterDiscovery
    {
        public Task<CharacterDiscoveryResult> DiscoverAsync(
            ITravelApi api,
            int concurrency,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CharacterDiscoveryResult(characters ?? [], []));
        }
    }

    private sealed class FakeTravelApi : ITravelApi
    {
        public IReadOnlyList<ActiveTravelOrder> ActiveTravelOrders { get; init; } = [];

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
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<ActiveTravelOrder>> GetActiveTravelOrdersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ActiveTravelOrders);
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
