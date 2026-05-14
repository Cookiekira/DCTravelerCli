using DCTravelCli.Domain;
using Spectre.Console;

namespace DCTravelCli.Ux;

public static class OrderTrackingOutput
{
    public static void WriteStatusChange(
        IAnsiConsole console,
        DateTimeOffset timestamp,
        MigrationStatus status)
    {
        console.WriteLine($"[{timestamp:HH:mm:ss}] {MigrationStatusText.Format(status)}");
    }
}
