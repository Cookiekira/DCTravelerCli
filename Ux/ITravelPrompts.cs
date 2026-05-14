using DCTravelerCli.Domain;

namespace DCTravelerCli.Ux;

public interface ITravelPrompts
{
    CharacterSelection SelectCharacter(IReadOnlyList<CharacterSelection> characters);

    ActiveTravelOrder SelectReturnOrder(IReadOnlyList<ActiveTravelOrder> orders);

    TargetRegion SelectTargetRegion(IReadOnlyList<TargetRegion> regions);

    TargetWorld SelectTargetWorld(TargetRegion region);

    bool ConfirmOrder(TravelSelection selection, bool assumeYes);

    bool ConfirmReturn(ActiveTravelOrder order, bool assumeYes);

    bool ConfirmReturnThenTravel(ReturnThenTravelSelection selection, bool assumeYes);

    bool ConfirmOfficialSecondStep();
}
