using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DCTravelCli.Commands;

public sealed class ReturnSettings : SessionSettings
{
    [CommandOption("-y|--yes")]
    [Description("跳过提交返回前确认。")]
    public bool Yes { get; set; }

    public override ValidationResult Validate() => ValidateSessionSettings();
}
