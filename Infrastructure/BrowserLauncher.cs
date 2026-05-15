using System.Diagnostics;
using System.Text.Json;
using DCTravelerCli.Domain;
using DCTravelerCli.Services;

namespace DCTravelerCli.Infrastructure;

internal sealed class BrowserLauncher(BrowserDiscovery browserDiscovery) : IDisposable
{
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<LaunchedBrowser> OpenAsync(
        SessionAcquisitionOptions options,
        string initialUrl,
        CancellationToken cancellationToken)
    {
        var process = await EnsureBrowserAsync(options, initialUrl, cancellationToken);
        await WaitForDebugEndpointAsync(options.DebugPort, cancellationToken);
        var page = await OpenDebugPageAsync(options.DebugPort, initialUrl, cancellationToken);
        return new LaunchedBrowser(process, page);
    }

    private async Task<Process?> EnsureBrowserAsync(
        SessionAcquisitionOptions options,
        string initialUrl,
        CancellationToken cancellationToken)
    {
        if (await TestDebugEndpointAsync(options.DebugPort, cancellationToken))
        {
            await AssertDebugEndpointUsableAsync(options.DebugPort, cancellationToken);
            return null;
        }

        if (!options.UseDefaultBrowserProfile)
        {
            Directory.CreateDirectory(options.ProfileDirectory);
        }

        var browserPath = browserDiscovery.ResolveBrowserPath(options.BrowserPath);

        var startInfo = new ProcessStartInfo(browserPath)
        {
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add($"--remote-debugging-port={options.DebugPort}");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--new-window");

        if (!options.UseDefaultBrowserProfile)
        {
            startInfo.ArgumentList.Add($"--user-data-dir={options.ProfileDirectory}");
        }

        startInfo.ArgumentList.Add(initialUrl);

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("无法启动浏览器。");
    }

    private async Task WaitForDebugEndpointAsync(int port, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(20);
        do
        {
            if (await TestDebugEndpointAsync(port, cancellationToken))
            {
                return;
            }

            await Task.Delay(300, cancellationToken);
        }
        while (DateTimeOffset.UtcNow < deadline);

        throw new InvalidOperationException($"无法连接浏览器调试端口 {port}。");
    }

    private async Task<bool> TestDebugEndpointAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync($"http://127.0.0.1:{port}/json/version", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task AssertDebugEndpointUsableAsync(int port, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"http://127.0.0.1:{port}/json/version", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(content);

        if (document.RootElement.TryGetProperty("Browser", out var browser) &&
            browser.ValueKind == JsonValueKind.String &&
            browser.GetString() is { } value &&
            !BrowserDiscovery.IsSupportedCdpBrowserProduct(value))
        {
            throw new InvalidOperationException($"调试端口 {port} 已被 {value} 占用。请关闭旧浏览器窗口，或使用 --debug-port 指定另一个端口。");
        }
    }

    private async Task<DebugPage> OpenDebugPageAsync(
        int port,
        string initialUrl,
        CancellationToken cancellationToken)
    {
        var pages = await GetPagesAsync(port, cancellationToken);
        var page = pages.FirstOrDefault(IsKnownLoginOrTravelPage);

        if (page is null)
        {
            var encodedUrl = Uri.EscapeDataString(initialUrl);
            using var response = await httpClient.PutAsync($"http://127.0.0.1:{port}/json/new?{encodedUrl}", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            await Task.Delay(800, cancellationToken);
            pages = await GetPagesAsync(port, cancellationToken);
            page = pages.FirstOrDefault(IsKnownLoginOrTravelPage);
        }

        page ??= pages.Count > 0 ? pages[0] : null;
        return page ?? throw new InvalidOperationException("未能取得页面调试 WebSocket。");
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    private async Task<IReadOnlyList<DebugPage>> GetPagesAsync(int port, CancellationToken cancellationToken)
    {
        var json = await httpClient.GetStringAsync($"http://127.0.0.1:{port}/json", cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return document.RootElement
            .EnumerateArray()
            .Where(page =>
                page.TryGetProperty("type", out var type) &&
                type.GetString() == "page" &&
                page.TryGetProperty("webSocketDebuggerUrl", out var webSocket) &&
                webSocket.ValueKind == JsonValueKind.String)
            .Select(page => new DebugPage(
                ReadString(page, "url"),
                ReadString(page, "webSocketDebuggerUrl")))
            .Where(page => !string.IsNullOrWhiteSpace(page.WebSocketDebuggerUrl))
            .ToArray();
    }

    private static bool IsKnownLoginOrTravelPage(DebugPage page)
    {
        return page.Url.StartsWith(OfficialEndpoints.BaseUrl, StringComparison.OrdinalIgnoreCase) ||
            page.Url.StartsWith("https://login.sdo.com", StringComparison.OrdinalIgnoreCase) ||
            page.Url.StartsWith("https://login.u.sdo.com", StringComparison.OrdinalIgnoreCase) ||
            page.Url.StartsWith("https://login.wegame.com.cn", StringComparison.OrdinalIgnoreCase) ||
            page.Url.StartsWith("https://api.rail.tgp.qq.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }
}
