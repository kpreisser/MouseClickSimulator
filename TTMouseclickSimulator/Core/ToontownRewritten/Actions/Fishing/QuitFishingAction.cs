﻿using System.Threading.Tasks;

using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing;

public class QuitFishingAction : AbstractAction
{
    public QuitFishingAction()
    {
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override sealed async ValueTask RunAsync(IInteractionProvider provider)
    {
        var c = new Coordinates(1503, 1086);
        await MouseHelpers.DoSimpleMouseClickAsync(provider, c);

        // Wait a bit and click again, to avoid the case when the button
        // would be disabled for a short time (due to catching a fish).
        await provider.WaitAsync(1000);
        await MouseHelpers.DoSimpleMouseClickAsync(provider, c);
    }

    public override string ToString()
    {
        return "Quit Fishing";
    }
}
