using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DCTravelCli.Commands;

public class SessionSettings : CommandSettings
{
    [CommandOption("--wegame")]
    [Description("通过盛趣官方跳转页直达 WeGame 登录。")]
    public bool WeGame { get; set; }

    [CommandOption("--keep-browser-open")]
    [Description("流程结束后保留 Chrome 窗口。")]
    public bool KeepBrowserOpen { get; set; }

    [CommandOption("--verbose")]
    [Description("显示诊断细节。")]
    public bool Verbose { get; set; }

    [CommandOption("--profile-dir <PATH>")]
    [Description("高级：专用 Chrome profile 路径。")]
    public string ProfileDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DCTravelCli",
        "ChromeProfile");

    [CommandOption("--debug-port <PORT>")]
    [Description("高级：Chrome DevTools 调试端口。")]
    [DefaultValue(43114)]
    public int DebugPort { get; set; } = 43114;

    [CommandOption("--login-timeout <SECONDS>")]
    [Description("高级：等待登录成功的秒数。")]
    [DefaultValue(600)]
    public int LoginTimeoutSeconds { get; set; } = 600;

    [CommandOption("--default-chrome-profile")]
    [Description("高级：使用 Chrome 默认 profile。")]
    public bool UseDefaultChromeProfile { get; set; }

    [CommandOption("--chrome-path <PATH>")]
    [Description("高级：手动指定 chrome.exe 路径。")]
    public string? ChromePath { get; set; }

    protected ValidationResult ValidateSessionSettings()
    {
        if (DebugPort is < 1024 or > 65535)
        {
            return ValidationResult.Error("--debug-port 必须在 1024 到 65535 之间。");
        }

        if (LoginTimeoutSeconds is < 30 or > 3600)
        {
            return ValidationResult.Error("--login-timeout 必须在 30 到 3600 秒之间。");
        }

        if (string.IsNullOrWhiteSpace(ProfileDirectory))
        {
            return ValidationResult.Error("--profile-dir 不能为空。");
        }

        return ValidationResult.Success();
    }

    public override ValidationResult Validate() => ValidateSessionSettings();
}
