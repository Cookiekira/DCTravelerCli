using System.Runtime.InteropServices;

namespace DCTravelerCli.Infrastructure;

internal sealed class BrowserDiscovery(BrowserDiscoveryEnvironment environment)
{
    public BrowserDiscovery()
        : this(BrowserDiscoveryEnvironment.Current)
    {
    }

    public string ResolveBrowserPath(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return environment.FileExists(explicitPath)
                ? explicitPath
                : throw new InvalidOperationException($"浏览器路径不存在：{explicitPath}");
        }

        return EnumerateCandidates()
            .FirstOrDefault(environment.FileExists)
            ?? throw new InvalidOperationException("未找到支持的 Chromium 系浏览器。请安装 Chrome、Edge、Chromium、Brave、Vivaldi、Opera 或 Arc，或使用 --browser-path 指定路径。");
    }

    public static bool IsSupportedCdpBrowserProduct(string product)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            return false;
        }

        var supportedPrefixes = new[]
        {
            "Chrome/",
            "HeadlessChrome/",
            "Chromium/",
            "Microsoft Edge/",
            "Edg/",
            "Brave/",
            "Vivaldi/",
            "Opera/",
            "OPR/",
            "Arc/"
        };

        return supportedPrefixes.Any(prefix => product.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<string> EnumerateCandidates()
    {
        return environment.Platform switch
        {
            BrowserPlatform.Windows => EnumerateWindowsCandidates(),
            BrowserPlatform.MacOS => EnumerateMacOSCandidates(),
            _ => EnumerateLinuxCandidates()
        };
    }

    private IEnumerable<string> EnumerateWindowsCandidates()
    {
        var roots = new[]
        {
            environment.ProgramFiles,
            environment.ProgramFilesX86,
            environment.LocalApplicationData
        }.Where(path => !string.IsNullOrWhiteSpace(path));

        foreach (var relativePath in WindowsRelativePaths)
        {
            foreach (var root in roots)
            {
                yield return Path.Combine(root, relativePath);
            }
        }
    }

    private IEnumerable<string> EnumerateMacOSCandidates()
    {
        foreach (var applicationsRoot in new[] { "/Applications", Path.Combine(environment.HomeDirectory, "Applications") })
        {
            foreach (var appName in MacOSApplicationNames)
            {
                yield return Path.Combine(applicationsRoot, $"{appName}.app", "Contents", "MacOS", appName);
            }
        }
    }

    private IEnumerable<string> EnumerateLinuxCandidates()
    {
        foreach (var executableName in LinuxExecutableNames)
        {
            var path = environment.FindOnPath(executableName);
            if (!string.IsNullOrWhiteSpace(path))
            {
                yield return path;
            }
        }
    }

    private static readonly string[] WindowsRelativePaths =
    [
        Path.Combine("Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine("Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine("Chromium", "Application", "chrome.exe"),
        Path.Combine("BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
        Path.Combine("Vivaldi", "Application", "vivaldi.exe"),
        Path.Combine("Opera", "opera.exe"),
        Path.Combine("Programs", "Opera", "opera.exe"),
        Path.Combine("Programs", "Arc", "Arc.exe")
    ];

    private static readonly string[] MacOSApplicationNames =
    [
        "Google Chrome",
        "Microsoft Edge",
        "Chromium",
        "Brave Browser",
        "Vivaldi",
        "Opera",
        "Arc"
    ];

    private static readonly string[] LinuxExecutableNames =
    [
        "google-chrome",
        "google-chrome-stable",
        "microsoft-edge",
        "microsoft-edge-stable",
        "chromium",
        "chromium-browser",
        "brave-browser",
        "vivaldi",
        "opera",
        "arc"
    ];
}

internal sealed record BrowserDiscoveryEnvironment(
    BrowserPlatform Platform,
    string ProgramFiles,
    string ProgramFilesX86,
    string LocalApplicationData,
    string HomeDirectory,
    Func<string, bool> FileExists,
    Func<string, string?> FindOnPath)
{
    public static BrowserDiscoveryEnvironment Current { get; } = new(
        GetCurrentPlatform(),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        File.Exists,
        FindExecutableOnPath);

    private static BrowserPlatform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return BrowserPlatform.Windows;
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? BrowserPlatform.MacOS
            : BrowserPlatform.Linux;
    }

    private static string? FindExecutableOnPath(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}

internal enum BrowserPlatform
{
    Windows,
    MacOS,
    Linux
}
