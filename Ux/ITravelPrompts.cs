using DCTravelCli.Domain;

namespace DCTravelCli.Ux;

public interface ITravelPrompts
{
    Character SelectCharacter(IReadOnlyList<Character> characters);

    TargetRegion SelectTargetRegion(IReadOnlyList<TargetRegion> regions);

    TargetWorld SelectTargetWorld(TargetRegion region);

    bool ConfirmOrder(TravelSelection selection, bool assumeYes);

    bool ConfirmOfficialSecondStep();
}
