using DCTravelerCli.Domain;

namespace DCTravelerCli.Tests;

public sealed class MigrationStatusTextTests
{
    [Theory]
    [InlineData(MigrationStatus.TeleportFailed, "传送失败")]
    [InlineData(MigrationStatus.PreCheckFailed, "预检查失败")]
    [InlineData(MigrationStatus.NeedConfirm, "需要二次确认")]
    [InlineData(MigrationStatus.Processing3, "排队中")]
    [InlineData(MigrationStatus.Completed, "已完成")]
    public void Format_maps_known_statuses(MigrationStatus status, string expected)
    {
        Assert.Equal(expected, MigrationStatusText.Format(status));
    }
}
