using DCTravelerCli.Domain;
using DCTravelerCli.Services;

namespace DCTravelerCli.Tests;

public sealed class CharacterDiscoveryTests
{
    [Fact]
    public async Task DiscoverAsync_keeps_characters_when_one_world_fails()
    {
        var region = new SourceRegion(
            1,
            "陆行鸟",
            [
                new SourceWorld(10, "A", "红玉海"),
                new SourceWorld(11, "B", "失败服")
            ]);
        var api = new FakeTravelApi([region])
        {
            CharacterResults =
            {
                [10] =
                [
                    new Character(
                        "100",
                        "Cookie",
                        region,
                        region.Worlds[0])
                ]
            },
            FailingWorldIds = { 11 }
        };

        var result = await new CharacterDiscovery().DiscoverAsync(api, concurrency: 4, CancellationToken.None);

        Assert.Single(result.Characters);
        Assert.Equal("Cookie", result.Characters[0].RoleName);
        Assert.Single(result.Failures);
        Assert.Equal("失败服", result.Failures[0].World.GroupName);
    }

    private sealed class FakeTravelApi(IReadOnlyList<SourceRegion> sourceRegions) : ICharacterDiscoveryApi
    {
        public Dictionary<int, IReadOnlyList<Character>> CharacterResults { get; } = [];

        public HashSet<int> FailingWorldIds { get; } = [];

        public Task<IReadOnlyList<SourceRegion>> GetSourceRegionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(sourceRegions);
        }

        public Task<IReadOnlyList<Character>> GetCharactersAsync(
            SourceRegion sourceRegion,
            SourceWorld sourceWorld,
            CancellationToken cancellationToken)
        {
            if (FailingWorldIds.Contains(sourceWorld.GroupId))
            {
                throw new InvalidOperationException("boom");
            }

            return Task.FromResult(
                CharacterResults.TryGetValue(sourceWorld.GroupId, out var characters)
                    ? characters
                    : []);
        }
    }
}
