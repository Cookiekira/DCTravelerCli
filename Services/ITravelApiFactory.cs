namespace DCTravelerCli.Services;

public interface ITravelApiFactory
{
    ITravelApi Create(OfficialSession session);
}
