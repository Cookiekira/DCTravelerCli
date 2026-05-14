using System.Diagnostics;

namespace DCTravelCli.Infrastructure;

internal sealed record LaunchedBrowser(Process? Process, DebugPage Page);
