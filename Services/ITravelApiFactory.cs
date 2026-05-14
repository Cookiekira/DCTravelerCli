namespace DCTravelCli.Services;

public interface ITravelApiFactory
{
    ITravelApi Create(OfficialSession session);
}
