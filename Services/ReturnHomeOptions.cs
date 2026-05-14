namespace DCTravelCli.Services;

public sealed record ReturnHomeOptions
{
    public int MaxAttempts { get; init; } = 3;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(65);

    public int StatusPollAttempts { get; init; } = 12;

    public TimeSpan StatusPollInterval { get; init; } = TimeSpan.FromSeconds(5);

    public bool Verbose { get; init; }
}
