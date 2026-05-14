using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DCTravelCli.Services;

namespace DCTravelCli.Infrastructure;

public sealed class FileSessionStore : ISessionStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DCTravelCli.session.v1");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string sessionPath;

    public FileSessionStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DCTravelCli",
            "session.dat"))
    {
    }

    public FileSessionStore(string sessionPath)
    {
        this.sessionPath = sessionPath;
    }

    public async Task<OfficialSession?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(sessionPath))
        {
            return null;
        }

        try
        {
            var protectedBytes = await File.ReadAllBytesAsync(sessionPath, cancellationToken);
            var jsonBytes = Unprotect(protectedBytes);
            var stored = JsonSerializer.Deserialize<StoredSession>(jsonBytes, JsonOptions);

            if (stored?.Cookies is null || stored.Cookies.Count == 0)
            {
                return null;
            }

            return OfficialSession.FromSourceCookies(stored.Cookies, stored.DisplayAccount);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(OfficialSession session, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(sessionPath) ?? ".");

        var stored = new StoredSession(
            Version: 1,
            SavedAt: DateTimeOffset.UtcNow,
            DisplayAccount: session.DisplayAccount,
            Cookies: session.SourceCookies);

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(stored, JsonOptions);
        var protectedBytes = Protect(jsonBytes);
        await File.WriteAllBytesAsync(sessionPath, protectedBytes, cancellationToken);
    }

    public Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(sessionPath))
        {
            File.Delete(sessionPath);
        }

        return Task.CompletedTask;
    }

    private static byte[] Protect(byte[] bytes)
    {
        return OperatingSystem.IsWindows()
            ? ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser)
            : bytes;
    }

    private static byte[] Unprotect(byte[] bytes)
    {
        return OperatingSystem.IsWindows()
            ? ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser)
            : bytes;
    }

    private sealed record StoredSession(
        int Version,
        DateTimeOffset SavedAt,
        string? DisplayAccount,
        IReadOnlyList<SessionCookie> Cookies);
}
