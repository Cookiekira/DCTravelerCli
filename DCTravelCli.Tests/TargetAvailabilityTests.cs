using DCTravelCli.Domain;
using DCTravelCli.Services;

namespace DCTravelCli.Tests;

public sealed class TargetAvailabilityTests
{
    [Fact]
    public void FilterAvailableTargets_removes_source_region_unavailable_regions_and_unavailable_worlds()
    {
        var sourceRegion = new TargetRegion(1, "源", [new TargetWorld(1, "S", "源服")]);
        var unavailableRegion = new TargetRegion(2, "不可用大区", [new TargetWorld(2, "U", "不可用服")], State: 2);
        var mixedRegion = new TargetRegion(
            3,
            "目标",
            [
                new TargetWorld(3, "A", "可用服"),
                new TargetWorld(4, "B", "不可用服", State: 2)
            ]);

        var result = TargetAvailability.FilterAvailableTargets(
            [sourceRegion, unavailableRegion, mixedRegion],
            sourceAreaId: 1);

        Assert.Single(result);
        Assert.Equal("目标", result[0].AreaName);
        Assert.Single(result[0].Worlds);
        Assert.Equal("可用服", result[0].Worlds[0].GroupName);
    }
}
