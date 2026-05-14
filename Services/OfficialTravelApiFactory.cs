namespace DCTravelCli.Services;

public sealed class OfficialTravelApiFactory : ITravelApiFactory
{
    public ITravelApi Create(OfficialSession session) => new OfficialTravelClient(session);
}
