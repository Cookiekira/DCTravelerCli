namespace DCTravelerCli.Domain;

public enum MigrationStatus
{
    TeleportFailed = -5,
    PreCheckFailed = -1,
    InPrepare0 = 0,
    InPrepare1 = 1,
    NeedConfirm = 2,
    Processing3 = 3,
    Processing4 = 4,
    Completed = 5
}

public static class MigrationStatusText
{
    public static string Format(MigrationStatus status) => status switch
    {
        MigrationStatus.TeleportFailed => "传送失败",
        MigrationStatus.PreCheckFailed => "预检查失败",
        MigrationStatus.InPrepare0 => "检查目标大区角色信息中",
        MigrationStatus.InPrepare1 => "检查目标大区角色信息中",
        MigrationStatus.NeedConfirm => "需要二次确认",
        MigrationStatus.Processing3 => "排队中",
        MigrationStatus.Processing4 => "排队中",
        MigrationStatus.Completed => "已完成",
        _ => $"未知状态 {(int)status}"
    };
}
