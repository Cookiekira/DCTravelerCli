using DCTravelerCli.Infrastructure;

namespace DCTravelerCli.Tests;

public sealed class BrowserDiscoveryTests
{
    [Fact]
    public void ResolveBrowserPath_prefers_explicit_path()
    {
        var explicitPath = Path.Combine("custom", "browser.exe");
        var chromePath = Path.Combine("ProgramFiles", "Google", "Chrome", "Application", "chrome.exe");
        var discovery = CreateDiscovery(
            BrowserPlatform.Windows,
            existingFiles: [explicitPath, chromePath]);

        Assert.Equal(explicitPath, discovery.ResolveBrowserPath(explicitPath));
    }

    [Fact]
    public void ResolveBrowserPath_rejects_invalid_explicit_path_instead_of_falling_back()
    {
        var chromePath = Path.Combine("ProgramFiles", "Google", "Chrome", "Application", "chrome.exe");
        var discovery = CreateDiscovery(
            BrowserPlatform.Windows,
            existingFiles: [chromePath]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            discovery.ResolveBrowserPath(Path.Combine("missing", "browser.exe")));

        Assert.Contains("浏览器路径不存在", exception.Message);
    }

    [Fact]
    public void ResolveBrowserPath_prefers_chrome_then_falls_back_to_edge_on_windows()
    {
        var edgePath = Path.Combine("ProgramFiles", "Microsoft", "Edge", "Application", "msedge.exe");
        var discovery = CreateDiscovery(
            BrowserPlatform.Windows,
            existingFiles: [edgePath]);

        Assert.Equal(edgePath, discovery.ResolveBrowserPath());
    }

    [Fact]
    public void ResolveBrowserPath_prefers_local_chrome_over_program_files_edge_on_windows()
    {
        var chromePath = Path.Combine("LocalAppData", "Google", "Chrome", "Application", "chrome.exe");
        var edgePath = Path.Combine("ProgramFiles", "Microsoft", "Edge", "Application", "msedge.exe");
        var discovery = CreateDiscovery(
            BrowserPlatform.Windows,
            existingFiles: [edgePath, chromePath]);

        Assert.Equal(chromePath, discovery.ResolveBrowserPath());
    }

    [Fact]
    public void ResolveBrowserPath_discovers_macos_chrome_under_applications()
    {
        var chromePath = Path.Combine(
            "/Applications",
            "Google Chrome.app",
            "Contents",
            "MacOS",
            "Google Chrome");
        var discovery = CreateDiscovery(
            BrowserPlatform.MacOS,
            existingFiles: [chromePath]);

        Assert.Equal(chromePath, discovery.ResolveBrowserPath());
    }

    [Fact]
    public void ResolveBrowserPath_discovers_macos_chrome_under_user_applications()
    {
        var chromePath = Path.Combine(
            "Home",
            "Applications",
            "Google Chrome.app",
            "Contents",
            "MacOS",
            "Google Chrome");
        var discovery = CreateDiscovery(
            BrowserPlatform.MacOS,
            existingFiles: [chromePath]);

        Assert.Equal(chromePath, discovery.ResolveBrowserPath());
    }

    [Fact]
    public void ResolveBrowserPath_uses_linux_path_names_in_preferred_order()
    {
        var discovery = CreateDiscovery(
            BrowserPlatform.Linux,
            existingFiles: ["/usr/bin/chromium", "/usr/bin/microsoft-edge"],
            pathExecutables: new Dictionary<string, string?>
            {
                ["chromium"] = "/usr/bin/chromium",
                ["microsoft-edge"] = "/usr/bin/microsoft-edge"
            });

        Assert.Equal("/usr/bin/microsoft-edge", discovery.ResolveBrowserPath());
    }

    [Theory]
    [InlineData("Chrome/125.0.6422.142")]
    [InlineData("HeadlessChrome/125.0.6422.142")]
    [InlineData("Chromium/125.0.6422.142")]
    [InlineData("Microsoft Edge/125.0.2535.92")]
    [InlineData("Brave/1.66.118")]
    [InlineData("Vivaldi/6.8.3381.46")]
    [InlineData("OPR/110.0.5130.23")]
    [InlineData("Arc/1.46.0")]
    public void IsSupportedCdpBrowserProduct_accepts_chromium_family_products(string product)
    {
        Assert.True(BrowserDiscovery.IsSupportedCdpBrowserProduct(product));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Firefox/126.0")]
    [InlineData("Node.js/v22.0.0")]
    public void IsSupportedCdpBrowserProduct_rejects_unknown_products(string product)
    {
        Assert.False(BrowserDiscovery.IsSupportedCdpBrowserProduct(product));
    }

    private static BrowserDiscovery CreateDiscovery(
        BrowserPlatform platform,
        IReadOnlyCollection<string>? existingFiles = null,
        IReadOnlyDictionary<string, string?>? pathExecutables = null)
    {
        var files = new HashSet<string>(existingFiles ?? [], StringComparer.OrdinalIgnoreCase);
        var environment = new BrowserDiscoveryEnvironment(
            platform,
            "ProgramFiles",
            "ProgramFilesX86",
            "LocalAppData",
            "Home",
            files.Contains,
            executableName => pathExecutables is not null && pathExecutables.TryGetValue(executableName, out var path)
                ? path
                : null);

        return new BrowserDiscovery(environment);
    }
}
