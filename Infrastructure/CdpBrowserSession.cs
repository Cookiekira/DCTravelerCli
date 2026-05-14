using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DCTravelCli.Infrastructure;

internal sealed class CdpBrowserSession : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ClientWebSocket socket;
    private int nextId;

    private CdpBrowserSession(ClientWebSocket socket)
    {
        this.socket = socket;
    }

    public static async Task<CdpBrowserSession> ConnectAsync(
        string webSocketDebuggerUrl,
        CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(webSocketDebuggerUrl), cancellationToken);

        var session = new CdpBrowserSession(socket);
        await session.InvokeAsync("Page.enable", new { }, cancellationToken);
        await session.InvokeAsync("Network.enable", new { }, cancellationToken);
        return session;
    }

    public Task NavigateAsync(string url, CancellationToken cancellationToken)
    {
        return InvokeAsync("Page.navigate", new { url }, cancellationToken);
    }

    public async Task<IReadOnlyList<CdpCookie>> GetCookiesAsync(
        IReadOnlyList<string> urls,
        CancellationToken cancellationToken)
    {
        using var result = await InvokeAsync("Network.getCookies", new { urls }, cancellationToken);
        if (!result.RootElement.TryGetProperty("cookies", out var cookiesElement) ||
            cookiesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return cookiesElement
            .EnumerateArray()
            .Select(cookie => new CdpCookie(
                ReadString(cookie, "name"),
                ReadString(cookie, "value"),
                ReadString(cookie, "domain"),
                ReadString(cookie, "path", "/"),
                ReadBool(cookie, "secure"),
                ReadBool(cookie, "httpOnly"),
                ReadDouble(cookie, "expires")))
            .Where(cookie => !string.IsNullOrWhiteSpace(cookie.Name))
            .ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        if (socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "done",
                CancellationToken.None);
        }

        socket.Dispose();
    }

    private async Task<JsonDocument> InvokeAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref nextId);
        var message = JsonSerializer.Serialize(
            new
            {
                id,
                method,
                @params = parameters
            },
            JsonOptions);

        await SendAsync(message, cancellationToken);

        while (true)
        {
            var response = await ReceiveAsync(cancellationToken);
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            if (!root.TryGetProperty("id", out var responseId) ||
                responseId.ValueKind != JsonValueKind.Number ||
                responseId.GetInt32() != id)
            {
                continue;
            }

            if (root.TryGetProperty("error", out var error))
            {
                throw new InvalidOperationException($"CDP 调用 {method} 失败：{error.GetRawText()}");
            }

            if (!root.TryGetProperty("result", out var result))
            {
                return JsonDocument.Parse("{}");
            }

            return JsonDocument.Parse(result.GetRawText());
        }
    }

    private async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await socket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    private async Task<string> ReceiveAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException("浏览器调试 WebSocket 已关闭。");
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback = "")
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.True;
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetDouble()
            : null;
    }
}
