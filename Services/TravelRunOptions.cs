namespace DCTravelerCli.Services;

public sealed record TravelRunOptions
{
    public required SessionAcquisitionOptions Session { get; init; }

    public bool AssumeYes { get; init; }

    public int DiscoveryConcurrency { get; init; } = 4;

    public bool Verbose { get; init; }

    public ReturnHomeOptions ReturnHome { get; init; } = new();

    public TimeSpan OrderTrackingPollInterval { get; init; } = TimeSpan.FromSeconds(3);
}
