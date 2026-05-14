namespace DCTravelCli.Infrastructure;

internal sealed record CdpCookie(
    string Name,
    string Value,
    string Domain,
    string Path,
    bool Secure,
    bool HttpOnly,
    double? Expires);
