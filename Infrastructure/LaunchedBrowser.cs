using System.Diagnostics;

namespace DCTravelerCli.Infrastructure;

internal sealed record LaunchedBrowser(Process? Process, DebugPage Page);
