using DCTravelerCli.Domain;
using DCTravelerCli.Services;

namespace DCTravelerCli.Tests;

public sealed class WeGameLoginNavigatorTests
{
    [Fact]
    public void BuildAutoLoginFrameUri_uses_official_shengqu_wegame_shortcut()
    {
        var uri = WeGameLoginNavigator.BuildAutoLoginFrameUri();

        Assert.Equal(OfficialEndpoints.LoginFrameUri.Host, uri.Host);
        Assert.Equal(OfficialEndpoints.LoginFrameUri.AbsolutePath, uri.AbsolutePath);
        Assert.Contains("goClick=wegame", uri.Query);
        Assert.Contains("thirdParty=wegame", uri.Query);
        Assert.Contains($"appId={OfficialEndpoints.AppId}", uri.Query);
        Assert.Contains("returnURL=https%3A%2F%2Fff14bjz.sdo.com%2FRegionKanTelepo", uri.Query);
    }

    [Fact]
    public void BuildAutoLoginFrameUri_does_not_directly_open_wegame_oauth()
    {
        var uri = WeGameLoginNavigator.BuildAutoLoginFrameUri();

        Assert.DoesNotContain("api.rail.tgp.qq.com", uri.AbsoluteUri);
        Assert.DoesNotContain("login.wegame.com.cn", uri.AbsoluteUri);
        Assert.DoesNotContain("oauth2.0", uri.AbsoluteUri);
    }
}
