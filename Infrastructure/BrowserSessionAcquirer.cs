using DCTravelerCli.Domain;
using DCTravelerCli.Services;
using Spectre.Console;

namespace DCTravelerCli.Infrastructure;

internal sealed class BrowserSessionAcquirer(
    BrowserLauncher launcher,
    ITravelApiFactory apiFactory,
    IWeGameLoginNavigator weGameLoginNavigator,
    ISessionStore sessionStore,
    IAnsiConsole console) : ISessionAcquirer
{
    private static readonly string[] CookieUrls =
    [
        OfficialEndpoints.BaseUrl,
        OfficialEndpoints.TravelUri.AbsoluteUri,
        "https://login.sdo.com",
        "https://login.u.sdo.com",
        "https://w.cas.sdo.com",
        "https://wcas.sdo.com"
    ];

    public async Task<OfficialSession> AcquireAsync(
        SessionAcquisitionOptions options,
        CancellationToken cancellationToken)
    {
        var loginUrl = SessionEntryPoint.GetInitialUrl(options);

        if (!options.ForceRefresh)
        {
            var storedSession = await TryLoadStoredSessionAsync(options, cancellationToken);
            if (storedSession is not null)
            {
                return storedSession;
            }
        }

        if (options.UseDefaultBrowserProfile)
        {
            console.MarkupLine("[yellow]正在使用默认浏览器 profile。请确保这是你信任的本机环境。[/]");
        }

        var launched = await launcher.OpenAsync(options, loginUrl, cancellationToken);
        await using var cdp = await CdpBrowserSession.ConnectAsync(launched.Page.WebSocketDebuggerUrl, cancellationToken);

        try
        {
            var existingSession = await TryCreateLoggedInSessionAsync(cdp, cancellationToken);
            if (existingSession is not null)
            {
                return await SaveAndReturnAsync(existingSession, cancellationToken);
            }

            await cdp.NavigateAsync(loginUrl, cancellationToken);
            if (options.PreferWeGameLogin)
            {
                var preparedLogin = weGameLoginNavigator.Prepare();
                await cdp.NavigateAsync(preparedLogin.LoginUri.AbsoluteUri, cancellationToken);
            }

            WriteLoginInstructions(options);

            var deadline = DateTimeOffset.UtcNow.AddSeconds(options.LoginTimeoutSeconds);
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                var session = await TryCreateLoggedInSessionAsync(cdp, cancellationToken);
                if (session is not null)
                {
                    return await SaveAndReturnAsync(session, cancellationToken);
                }
            }
            while (DateTimeOffset.UtcNow < deadline);

            throw new TimeoutException("等待登录超时。");
        }
        finally
        {
            if (launched.Process is { HasExited: false } process && !options.KeepBrowserOpen)
            {
                process.CloseMainWindow();
            }
        }
    }

    private async Task<OfficialSession?> TryLoadStoredSessionAsync(
        SessionAcquisitionOptions options,
        CancellationToken cancellationToken)
    {
        var storedSession = await sessionStore.LoadAsync(cancellationToken);
        if (storedSession is null)
        {
            return null;
        }

        var validSession = await TryValidateSessionAsync(storedSession, cancellationToken);
        if (validSession is not null)
        {
            if (options.Verbose)
            {
                console.MarkupLine("[grey]已使用保存的登录会话。[/]");
            }

            return validSession;
        }

        await sessionStore.DeleteAsync(cancellationToken);
        if (options.Verbose)
        {
            console.MarkupLine("[grey]保存的登录会话已失效，准备重新登录。[/]");
        }

        return null;
    }

    private async Task<OfficialSession> SaveAndReturnAsync(
        OfficialSession session,
        CancellationToken cancellationToken)
    {
        await sessionStore.SaveAsync(session, cancellationToken);
        return session;
    }

    private void WriteLoginInstructions(SessionAcquisitionOptions options)
    {
        console.WriteLine();
        console.MarkupLine($"[cyan]{Markup.Escape(SessionEntryPoint.GetInstruction(options))}[/]");

        console.MarkupLine(options.UseDefaultBrowserProfile
            ? "CLI 会等待登录成功，并使用默认浏览器 profile 的会话。"
            : $"CLI 会等待登录成功，专用 profile 位于：[grey]{Markup.Escape(options.ProfileDirectory)}[/]");
        console.WriteLine();
    }

    private async Task<OfficialSession?> TryCreateLoggedInSessionAsync(
        CdpBrowserSession cdp,
        CancellationToken cancellationToken)
    {
        var cdpCookies = await cdp.GetCookiesAsync(CookieUrls, cancellationToken);
        var session = CreateOfficialSession(cdpCookies);

        var hasLoginCookie = cdpCookies.Any(cookie => cookie.Name == "isLogin" && cookie.Value == "1");
        if (hasLoginCookie)
        {
            return session;
        }

        using var api = apiFactory.Create(session);
        var probe = await api.ProbeLoginAsync(cancellationToken);
        return BuildValidatedSession(session, probe);
    }

    private async Task<OfficialSession?> TryValidateSessionAsync(
        OfficialSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            using var api = apiFactory.Create(session);
            var probe = await api.ProbeLoginAsync(cancellationToken);
            return BuildValidatedSession(session, probe);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private static OfficialSession? BuildValidatedSession(
        OfficialSession session,
        LoginProbe probe)
    {
        if (!probe.IsLoggedIn)
        {
            return null;
        }

        return session with
        {
            DisplayAccount = string.IsNullOrWhiteSpace(session.DisplayAccount)
                ? probe.DisplayAccount
                : session.DisplayAccount
        };
    }

    private static OfficialSession CreateOfficialSession(IReadOnlyList<CdpCookie> cdpCookies)
    {
        var sourceCookies = new List<SessionCookie>(cdpCookies.Count);

        foreach (var cdpCookie in cdpCookies)
        {
            if (string.IsNullOrWhiteSpace(cdpCookie.Name))
            {
                continue;
            }

            sourceCookies.Add(new SessionCookie(
                cdpCookie.Name,
                cdpCookie.Value,
                cdpCookie.Domain,
                cdpCookie.Path,
                cdpCookie.Secure,
                cdpCookie.HttpOnly,
                cdpCookie.Expires is > 0
                    ? DateTimeOffset.FromUnixTimeSeconds((long)cdpCookie.Expires.Value)
                    : null));
        }

        return OfficialSession.FromSourceCookies(sourceCookies);
    }
}
