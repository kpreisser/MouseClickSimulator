using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;
using TTMouseClickSimulator.Core.Toontown.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Fishing;

public class SellFishAction : AbstractAction
{
    public SellFishAction()
    {
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        var ttProvider = (ToontownInteractionProvider)provider;
        var c = ttProvider.ToontownFlavor is ToontownFlavor.CorporateClash ?
            new Coordinates(1159, 907) :
            new Coordinates(1159, 911);

        MouseHelpers.DoSimpleMouseClick(provider, c);
    }

    public override string ToString()
    {
        return "Sell Fish";
    }
}
