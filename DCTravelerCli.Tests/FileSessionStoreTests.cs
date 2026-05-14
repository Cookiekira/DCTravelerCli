using DCTravelerCli.Infrastructure;
using DCTravelerCli.Services;

namespace DCTravelerCli.Tests;

public sealed class FileSessionStoreTests
{
    [Fact]
    public async Task LoadAsync_returns_saved_session()
    {
        var sessionPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.session");
        var store = new FileSessionStore(sessionPath);
        var session = OfficialSession.FromSourceCookies(
            [
                new SessionCookie(
                    "isLogin",
                    "1",
                    ".sdo.com",
                    "/",
                    Secure: true,
                    HttpOnly: false,
                    Expires: DateTimeOffset.Now.AddDays(1)),
                new SessionCookie(
                    "displayAccount",
                    Uri.EscapeDataString("76561197986149273"),
                    ".sdo.com",
                    "/",
                    Secure: true,
                    HttpOnly: false,
                    Expires: DateTimeOffset.Now.AddDays(1))
            ]);

        try
        {
            await store.SaveAsync(session, CancellationToken.None);
            var loaded = await store.LoadAsync(CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal("76561197986149273", loaded.DisplayAccount);
            Assert.Contains(loaded.SourceCookies, cookie => cookie.Name == "isLogin" && cookie.Value == "1");
        }
        finally
        {
            if (File.Exists(sessionPath))
            {
                File.Delete(sessionPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_returns_null_when_session_file_is_missing()
    {
        var sessionPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.session");
        var store = new FileSessionStore(sessionPath);

        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Null(loaded);
    }
}
