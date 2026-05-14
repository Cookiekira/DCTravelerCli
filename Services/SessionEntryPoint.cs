using DCTravelerCli.Domain;

namespace DCTravelerCli.Services;

public static class SessionEntryPoint
{
    public static string GetInitialUrl(SessionAcquisitionOptions options)
    {
        return OfficialEndpoints.TravelUri.AbsoluteUri;
    }

    public static string GetInstruction(SessionAcquisitionOptions options)
    {
        return options.PreferWeGameLogin
            ? "已打开盛趣官方 WeGame 跳转页。请完成 QQ/WeGame 认证。"
            : "已打开超域传送页面。请在页面中手动登录。";
    }
}
