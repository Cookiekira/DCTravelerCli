using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public sealed class WeGameLoginNavigator : IWeGameLoginNavigator
{
    private const string EncodedTarget = "top%810%853_param=from%3D78";

    public PreparedWeGameLogin Prepare()
    {
        return new PreparedWeGameLogin(BuildAutoLoginFrameUri());
    }

    public static Uri BuildAutoLoginFrameUri()
    {
        var travelUrl = Uri.EscapeDataString(OfficialEndpoints.TravelUri.AbsoluteUri);
        return new Uri(
            $"{OfficialEndpoints.LoginFrameUri.AbsoluteUri}" +
            $"?pm=2" +
            $"&appId={OfficialEndpoints.AppId}" +
            $"&areaId=1001" +
            $"&customSecurityLevel=2" +
            $"&target={EncodedTarget}" +
            $"&thirdParty=wegame" +
            $"&goClick=wegame" +
            $"&returnURL={travelUrl}" +
            $"&backUrl={travelUrl}");
    }
}
