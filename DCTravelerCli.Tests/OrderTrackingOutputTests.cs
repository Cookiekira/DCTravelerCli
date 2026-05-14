using DCTravelerCli.Domain;
using DCTravelerCli.Ux;
using Spectre.Console.Testing;

namespace DCTravelerCli.Tests;

public sealed class OrderTrackingOutputTests
{
    [Fact]
    public void WriteStatusChange_writes_timestamp_without_markup_parsing()
    {
        var console = new TestConsole();
        var timestamp = new DateTimeOffset(2026, 5, 14, 9, 24, 55, TimeSpan.Zero);

        var exception = Record.Exception(() =>
            OrderTrackingOutput.WriteStatusChange(console, timestamp, MigrationStatus.Completed));

        Assert.Null(exception);
        Assert.Contains("[09:24:55] 已完成", console.Output);
    }
}
