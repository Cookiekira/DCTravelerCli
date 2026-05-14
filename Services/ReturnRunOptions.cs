namespace DCTravelerCli.Services;

public sealed record ReturnRunOptions
{
    public required SessionAcquisitionOptions Session { get; init; }

    public bool AssumeYes { get; init; }

    public bool Verbose { get; init; }

    public ReturnHomeOptions ReturnHome { get; init; } = new();
}
