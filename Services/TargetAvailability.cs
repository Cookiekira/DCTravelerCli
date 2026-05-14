using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public static class TargetAvailability
{
    private const int OfficialUnavailableState = 2;

    public static IReadOnlyList<TargetRegion> FilterAvailableTargets(
        IEnumerable<TargetRegion> regions,
        int sourceAreaId)
    {
        return regions
            .Where(region => region.AreaId != sourceAreaId && !IsUnavailable(region.State))
            .Select(region => region with
            {
                Worlds = region.Worlds
                    .Where(world => !IsUnavailable(world.State))
                    .ToArray()
            })
            .Where(region => region.Worlds.Count > 0)
            .ToArray();
    }

    public static bool IsUnavailable(int? state) => state == OfficialUnavailableState;
}
