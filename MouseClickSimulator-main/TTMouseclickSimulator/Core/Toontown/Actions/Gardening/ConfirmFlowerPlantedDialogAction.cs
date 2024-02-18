using System;

using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;
using TTMouseClickSimulator.Core.Toontown.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Gardening;

/// <summary>
/// An action which confirms the "You just planted ..." dialog.
/// </summary>
public class ConfirmFlowerPlantedDialogAction : AbstractAction
{
    public ConfirmFlowerPlantedDialogAction()
    {
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        provider.ThrowIfNotToontownRewritten(nameof(ConfirmFlowerPlantedDialogAction));

        // Click on the "Ok" button.
        // Note: Depending on the name of the flower, the button may be at two different
        // locations (712 and 727). Therefore, we click on the middle of these to hit the
        // button in both cases.
        MouseHelpers.DoSimpleMouseClick(provider, new Coordinates(800, 720));
    }

    public override string ToString()
    {
        return $"Confirm \"You just planted ...\" dialog";
    }
}
