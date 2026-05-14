using DCTravelCli.Domain;
using DCTravelCli.Services;

namespace DCTravelCli.Tests;

public sealed class SessionEntryPointTests
{
    [Fact]
    public void GetInitialUrl_opens_travel_page_for_normal_login()
    {
        var options = new SessionAcquisitionOptions
        {
            ProfileDirectory = "profile"
        };

        Assert.Equal(OfficialEndpoints.TravelUri.AbsoluteUri, SessionEntryPoint.GetInitialUrl(options));
    }

    [Fact]
    public void GetInitialUrl_opens_travel_page_for_wegame_login_shortcut()
    {
        var options = new SessionAcquisitionOptions
        {
            ProfileDirectory = "profile",
            PreferWeGameLogin = true
        };

        Assert.Equal(OfficialEndpoints.TravelUri.AbsoluteUri, SessionEntryPoint.GetInitialUrl(options));
    }

    [Fact]
    public void GetInstruction_tells_user_that_official_wegame_shortcut_is_opened()
    {
        var options = new SessionAcquisitionOptions
        {
            ProfileDirectory = "profile",
            PreferWeGameLogin = true
        };

        var instruction = SessionEntryPoint.GetInstruction(options);

        Assert.Contains("盛趣官方 WeGame 跳转页", instruction);
    }
}
