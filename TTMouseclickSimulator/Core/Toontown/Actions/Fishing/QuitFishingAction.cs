using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;
using TTMouseClickSimulator.Core.Toontown.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Fishing;

public class QuitFishingAction : AbstractAction
{
    public QuitFishingAction()
    {
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        var ttProvider = (ToontownInteractionProvider)provider;
        var (c, alignment) = ttProvider.ToontownFlavor is ToontownFlavor.CorporateClash ?
            (new Coordinates(1511, 1084), HorizontalScaleAlignment.Right) :
            (new Coordinates(1503, 1086), HorizontalScaleAlignment.Center);

        MouseHelpers.DoSimpleMouseClick(provider, c, alignment);

        // Wait a bit and click again, to avoid the case when the button would be
        // disabled for a short time (due to catching a fish).
        // (But actually this shouldn't be necessary as that button always works.)
        provider.Wait(1000);
        MouseHelpers.DoSimpleMouseClick(provider, c, alignment);
    }

    public override string ToString()
    {
        return "Quit Fishing";
    }
}
