using ConsoleAppFramework;
using DCTravelerCli.Services;
using Spectre.Console;

namespace DCTravelerCli.Commands;

public sealed class CliCommands(
    ITravelFlow travelFlow,
    IReturnFlow returnFlow,
    ISessionAcquirer sessionAcquirer,
    IAnsiConsole console)
{
    private static readonly string DefaultProfileDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DCTravelerCli",
        "BrowserProfile");

    /// <summary>一键执行 FF14 国服超域传送流程。</summary>
    /// <param name="wegame">通过盛趣官方跳转页直达 WeGame 登录。</param>
    /// <param name="keepBrowserOpen">流程结束后保留浏览器窗口。</param>
    /// <param name="verbose">显示诊断细节。</param>
    /// <param name="profileDir">高级：专用浏览器 profile 路径。</param>
    /// <param name="debugPort">高级：浏览器 DevTools 调试端口。</param>
    /// <param name="loginTimeout">高级：等待登录成功的秒数。</param>
    /// <param name="defaultBrowserProfile">高级：使用默认浏览器 profile。</param>
    /// <param name="browserPath">高级：手动指定浏览器可执行文件路径。</param>
    /// <param name="yes">-y, 跳过提交前确认。</param>
    /// <param name="discoveryConcurrency">高级：角色发现并发数。</param>
    [Command("")]
    public Task Run(
        bool wegame = false,
        bool keepBrowserOpen = false,
        bool verbose = false,
        [HideDefaultValue] string? profileDir = null,
        int debugPort = 43114,
        int loginTimeout = 600,
        bool defaultBrowserProfile = false,
        [HideDefaultValue] string? browserPath = null,
        bool yes = false,
        int discoveryConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        return RunTravelAsync(
            wegame,
            keepBrowserOpen,
            verbose,
            profileDir,
            debugPort,
            loginTimeout,
            defaultBrowserProfile,
            browserPath,
            yes,
            discoveryConcurrency,
            cancellationToken);
    }

    /// <summary>一键执行 FF14 国服超域传送流程。</summary>
    /// <param name="wegame">通过盛趣官方跳转页直达 WeGame 登录。</param>
    /// <param name="keepBrowserOpen">流程结束后保留浏览器窗口。</param>
    /// <param name="verbose">显示诊断细节。</param>
    /// <param name="profileDir">高级：专用浏览器 profile 路径。</param>
    /// <param name="debugPort">高级：浏览器 DevTools 调试端口。</param>
    /// <param name="loginTimeout">高级：等待登录成功的秒数。</param>
    /// <param name="defaultBrowserProfile">高级：使用默认浏览器 profile。</param>
    /// <param name="browserPath">高级：手动指定浏览器可执行文件路径。</param>
    /// <param name="yes">-y, 跳过提交前确认。</param>
    /// <param name="discoveryConcurrency">高级：角色发现并发数。</param>
    [Command("travel")]
    public Task Travel(
        bool wegame = false,
        bool keepBrowserOpen = false,
        bool verbose = false,
        [HideDefaultValue] string? profileDir = null,
        int debugPort = 43114,
        int loginTimeout = 600,
        bool defaultBrowserProfile = false,
        [HideDefaultValue] string? browserPath = null,
        bool yes = false,
        int discoveryConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        return RunTravelAsync(
            wegame,
            keepBrowserOpen,
            verbose,
            profileDir,
            debugPort,
            loginTimeout,
            defaultBrowserProfile,
            browserPath,
            yes,
            discoveryConcurrency,
            cancellationToken);
    }

    /// <summary>只刷新登录会话，不提交超域传送订单。</summary>
    /// <param name="wegame">通过盛趣官方跳转页直达 WeGame 登录。</param>
    /// <param name="keepBrowserOpen">流程结束后保留浏览器窗口。</param>
    /// <param name="verbose">显示诊断细节。</param>
    /// <param name="profileDir">高级：专用浏览器 profile 路径。</param>
    /// <param name="debugPort">高级：浏览器 DevTools 调试端口。</param>
    /// <param name="loginTimeout">高级：等待登录成功的秒数。</param>
    /// <param name="defaultBrowserProfile">高级：使用默认浏览器 profile。</param>
    /// <param name="browserPath">高级：手动指定浏览器可执行文件路径。</param>
    public async Task Login(
        bool wegame = false,
        bool keepBrowserOpen = false,
        bool verbose = false,
        [HideDefaultValue] string? profileDir = null,
        int debugPort = 43114,
        int loginTimeout = 600,
        bool defaultBrowserProfile = false,
        [HideDefaultValue] string? browserPath = null,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionAcquirer.AcquireAsync(
            ToSessionOptions(
                wegame,
                keepBrowserOpen,
                verbose,
                profileDir,
                debugPort,
                loginTimeout,
                defaultBrowserProfile,
                browserPath,
                forceRefresh: true),
            cancellationToken);

        console.MarkupLine(!string.IsNullOrWhiteSpace(session.DisplayAccount)
            ? $"登录会话已刷新：[green]{Markup.Escape(session.DisplayAccount)}[/]"
            : "[green]登录会话已刷新。[/]");
    }

    /// <summary>将旅行中的 FF14 角色返回原服。</summary>
    /// <param name="wegame">通过盛趣官方跳转页直达 WeGame 登录。</param>
    /// <param name="keepBrowserOpen">流程结束后保留浏览器窗口。</param>
    /// <param name="verbose">显示诊断细节。</param>
    /// <param name="profileDir">高级：专用浏览器 profile 路径。</param>
    /// <param name="debugPort">高级：浏览器 DevTools 调试端口。</param>
    /// <param name="loginTimeout">高级：等待登录成功的秒数。</param>
    /// <param name="defaultBrowserProfile">高级：使用默认浏览器 profile。</param>
    /// <param name="browserPath">高级：手动指定浏览器可执行文件路径。</param>
    /// <param name="yes">-y, 跳过提交返回前确认。</param>
    [Command("return")]
    public Task Return(
        bool wegame = false,
        bool keepBrowserOpen = false,
        bool verbose = false,
        [HideDefaultValue] string? profileDir = null,
        int debugPort = 43114,
        int loginTimeout = 600,
        bool defaultBrowserProfile = false,
        [HideDefaultValue] string? browserPath = null,
        bool yes = false,
        CancellationToken cancellationToken = default)
    {
        return returnFlow.RunAsync(
            new ReturnRunOptions
            {
                Session = ToSessionOptions(
                    wegame,
                    keepBrowserOpen,
                    verbose,
                    profileDir,
                    debugPort,
                    loginTimeout,
                    defaultBrowserProfile,
                    browserPath),
                AssumeYes = yes,
                Verbose = verbose,
                ReturnHome = new ReturnHomeOptions
                {
                    Verbose = verbose
                }
            },
            cancellationToken);
    }

    private Task RunTravelAsync(
        bool wegame,
        bool keepBrowserOpen,
        bool verbose,
        string? profileDir,
        int debugPort,
        int loginTimeout,
        bool defaultBrowserProfile,
        string? browserPath,
        bool yes,
        int discoveryConcurrency,
        CancellationToken cancellationToken)
    {
        if (discoveryConcurrency is < 1 or > 16)
        {
            throw new ArgumentException("--discovery-concurrency 必须在 1 到 16 之间。");
        }

        return travelFlow.RunAsync(
            new TravelRunOptions
            {
                Session = ToSessionOptions(
                    wegame,
                    keepBrowserOpen,
                    verbose,
                    profileDir,
                    debugPort,
                    loginTimeout,
                    defaultBrowserProfile,
                    browserPath),
                AssumeYes = yes,
                DiscoveryConcurrency = discoveryConcurrency,
                Verbose = verbose,
                ReturnHome = new ReturnHomeOptions
                {
                    Verbose = verbose
                }
            },
            cancellationToken);
    }

    internal static SessionAcquisitionOptions ToSessionOptions(
        bool wegame,
        bool keepBrowserOpen,
        bool verbose,
        string? profileDir,
        int debugPort,
        int loginTimeout,
        bool defaultBrowserProfile,
        string? browserPath,
        bool forceRefresh = false)
    {
        if (debugPort is < 1024 or > 65535)
        {
            throw new ArgumentException("--debug-port 必须在 1024 到 65535 之间。");
        }

        if (loginTimeout is < 30 or > 3600)
        {
            throw new ArgumentException("--login-timeout 必须在 30 到 3600 秒之间。");
        }

        var resolvedProfileDir = profileDir ?? DefaultProfileDirectory;
        if (string.IsNullOrWhiteSpace(resolvedProfileDir))
        {
            throw new ArgumentException("--profile-dir 不能为空。");
        }

        return new SessionAcquisitionOptions
        {
            ProfileDirectory = resolvedProfileDir,
            DebugPort = debugPort,
            LoginTimeoutSeconds = loginTimeout,
            KeepBrowserOpen = keepBrowserOpen,
            UseDefaultBrowserProfile = defaultBrowserProfile,
            PreferWeGameLogin = wegame,
            BrowserPath = browserPath,
            Verbose = verbose,
            ForceRefresh = forceRefresh
        };
    }
}
