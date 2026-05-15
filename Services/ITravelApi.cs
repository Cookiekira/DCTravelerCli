using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public interface ILoginProbeApi
{
    Task<LoginProbe> ProbeLoginAsync(CancellationToken cancellationToken);
}

public interface ICharacterDiscoveryApi
{
    Task<IReadOnlyList<SourceRegion>> GetSourceRegionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Character>> GetCharactersAsync(
        SourceRegion sourceRegion,
        SourceWorld sourceWorld,
        CancellationToken cancellationToken);
}

public interface ICharacterSelectionCatalogApi : ICharacterDiscoveryApi
{
    Task<IReadOnlyList<ActiveTravelOrder>> GetActiveTravelOrdersAsync(CancellationToken cancellationToken);
}

public interface ITargetAvailabilityApi
{
    Task<IReadOnlyList<TargetRegion>> GetTargetRegionsAsync(
        SourceRegion sourceRegion,
        SourceWorld sourceWorld,
        CancellationToken cancellationToken);
}

public interface IReturnHomeApi
{
    Task<IReadOnlyList<MigrationOrderSummary>> GetMigrationOrdersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SourceRegion>> GetReturnSourceRegionsAsync(CancellationToken cancellationToken);

    Task<ReturnHomeOrder> SubmitReturnHomeAsync(
        ReturnHomeSelection selection,
        CancellationToken cancellationToken);
}

public interface ITravelOrderApi
{
    Task<TravelOrder> SubmitTravelOrderAsync(
        TravelSelection selection,
        CancellationToken cancellationToken);

    Task<OrderStatusSnapshot> GetOrderStatusAsync(
        TravelOrder order,
        CancellationToken cancellationToken);

    Task ConfirmOrderAsync(
        TravelOrder order,
        bool confirm,
        CancellationToken cancellationToken);
}

public interface ITravelApi :
    ILoginProbeApi,
    ICharacterSelectionCatalogApi,
    ITargetAvailabilityApi,
    IReturnHomeApi,
    ITravelOrderApi,
    IDisposable;
