namespace DCTravelerCli.Services;

public sealed record SessionAcquisitionOptions
{
    public required string ProfileDirectory { get; init; }

    public int DebugPort { get; init; } = 43114;

    public int LoginTimeoutSeconds { get; init; } = 600;

    public bool KeepBrowserOpen { get; init; }

    public bool UseDefaultChromeProfile { get; init; }

    public bool PreferWeGameLogin { get; init; }

    public string? ChromePath { get; init; }

    public bool Verbose { get; init; }

    public bool ForceRefresh { get; init; }
}
