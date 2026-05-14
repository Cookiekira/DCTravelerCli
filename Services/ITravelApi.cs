using DCTravelCli.Domain;

namespace DCTravelCli.Services;

public interface ITravelApi : IDisposable
{
    Task<LoginProbe> ProbeLoginAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SourceRegion>> GetSourceRegionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Character>> GetCharactersAsync(
        SourceRegion sourceRegion,
        SourceWorld sourceWorld,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TargetRegion>> GetTargetRegionsAsync(
        SourceRegion sourceRegion,
        SourceWorld sourceWorld,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MigrationOrderSummary>> GetMigrationOrdersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ActiveTravelOrder>> GetActiveTravelOrdersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SourceRegion>> GetReturnSourceRegionsAsync(CancellationToken cancellationToken);

    Task<ReturnHomeOrder> SubmitReturnHomeAsync(
        ReturnHomeSelection selection,
        CancellationToken cancellationToken);

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
