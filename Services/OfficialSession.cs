using System.Net;
using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public sealed record OfficialSession(
    CookieContainer Cookies,
    string? DisplayAccount,
    IReadOnlyList<SessionCookie> SourceCookies)
{
    public static OfficialSession FromSourceCookies(
        IReadOnlyList<SessionCookie> sourceCookies,
        string? displayAccount = null)
    {
        var container = new CookieContainer();
        var validCookies = sourceCookies
            .Where(cookie => cookie.Expires is null || cookie.Expires > DateTimeOffset.Now)
            .ToArray();

        foreach (var sourceCookie in validCookies)
        {
            var cookie = new Cookie(
                sourceCookie.Name,
                sourceCookie.Value,
                string.IsNullOrWhiteSpace(sourceCookie.Path) ? "/" : sourceCookie.Path)
            {
                Secure = sourceCookie.Secure,
                HttpOnly = sourceCookie.HttpOnly
            };

            if (!string.IsNullOrWhiteSpace(sourceCookie.Domain))
            {
                cookie.Domain = sourceCookie.Domain;
            }

            if (sourceCookie.Expires is not null)
            {
                cookie.Expires = sourceCookie.Expires.Value.UtcDateTime;
            }

            AddCookie(container, cookie);
        }

        displayAccount ??= validCookies
            .FirstOrDefault(cookie => cookie.Name == "displayAccount")
            ?.Value;
        if (!string.IsNullOrWhiteSpace(displayAccount))
        {
            displayAccount = Uri.UnescapeDataString(displayAccount);
        }

        return new OfficialSession(container, displayAccount, validCookies);
    }

    private static void AddCookie(CookieContainer container, Cookie cookie)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(cookie.Domain))
            {
                container.Add(cookie);
                return;
            }
        }
        catch (CookieException)
        {
            // Fall through to host-scoped fallback below.
        }

        container.Add(
            OfficialEndpoints.BaseUri,
            new Cookie(cookie.Name, cookie.Value, cookie.Path)
            {
                Secure = cookie.Secure,
                HttpOnly = cookie.HttpOnly,
                Expires = cookie.Expires
            });
    }
}

public sealed record SessionCookie(
    string Name,
    string Value,
    string Domain,
    string Path,
    bool Secure,
    bool HttpOnly,
    DateTimeOffset? Expires);
