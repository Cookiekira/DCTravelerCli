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
        Character character,
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
