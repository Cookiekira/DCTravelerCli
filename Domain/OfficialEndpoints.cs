namespace DCTravelCli.Domain;

public static class OfficialEndpoints
{
    public const int AppId = 100001900;
    public const int MigrationType = 4;

    public static readonly Uri BaseUri = new("https://ff14bjz.sdo.com");
    public static readonly Uri TravelUri = new(BaseUri, "/RegionKanTelepo");
    public static readonly Uri LoginFrameUri = new("https://login.sdo.com/sdo/Login/LoginFrameFC.php");

    public static string BaseUrl => BaseUri.GetLeftPart(UriPartial.Authority);
}
