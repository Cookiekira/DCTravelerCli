using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DCTravelCli.Commands;

public sealed class TravelSettings : SessionSettings
{
    [CommandOption("-y|--yes")]
    [Description("跳过提交前确认。")]
    public bool Yes { get; set; }

    [CommandOption("--discovery-concurrency <COUNT>")]
    [Description("高级：角色发现并发数。")]
    [DefaultValue(4)]
    public int DiscoveryConcurrency { get; set; } = 4;

    public override ValidationResult Validate()
    {
        var sessionResult = ValidateSessionSettings();
        if (!sessionResult.Successful)
        {
            return sessionResult;
        }

        if (DiscoveryConcurrency is < 1 or > 16)
        {
            return ValidationResult.Error("--discovery-concurrency 必须在 1 到 16 之间。");
        }

        return ValidationResult.Success();
    }
}
