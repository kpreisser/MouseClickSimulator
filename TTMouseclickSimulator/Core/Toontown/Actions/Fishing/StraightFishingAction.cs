using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Fishing;

public class StraightFishingAction : AbstractFishingRodAction
{
    public StraightFishingAction()
    {
    }

    protected override int WaitingForFishResultDialogTime
    {
        get => 25000;
    }

    protected override sealed void FinishCastFishingRod(IInteractionProvider provider)
    {
        // Simply cast the fishing rod straight, without checking for bubbles.
        var coords = new Coordinates(800, 1009);
        var pos = provider.GetCurrentWindowPosition();

        coords = pos.ScaleCoordinates(
            coords,
            MouseHelpers.ReferenceWindowSize);

        provider.MoveMouse(coords);
        provider.Wait(300);
        provider.ReleaseMouseButton();
    }

    public override string ToString()
    {
        return "Straight Fishing Cast";
    }
}
