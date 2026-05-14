using DCTravelCli.Services;

namespace DCTravelCli.Commands;

internal static class SettingsMapper
{
    public static SessionAcquisitionOptions ToSessionOptions(
        SessionSettings settings,
        bool forceRefresh = false)
    {
        return new SessionAcquisitionOptions
        {
            ProfileDirectory = settings.ProfileDirectory,
            DebugPort = settings.DebugPort,
            LoginTimeoutSeconds = settings.LoginTimeoutSeconds,
            KeepBrowserOpen = settings.KeepBrowserOpen,
            UseDefaultChromeProfile = settings.UseDefaultChromeProfile,
            PreferWeGameLogin = settings.WeGame,
            ChromePath = settings.ChromePath,
            Verbose = settings.Verbose,
            ForceRefresh = forceRefresh
        };
    }
}
