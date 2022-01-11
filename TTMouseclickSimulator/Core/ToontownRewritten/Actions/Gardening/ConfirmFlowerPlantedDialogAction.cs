using System.Threading.Tasks;

using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Gardening;

/// <summary>
/// An action which confirms the "You just planted ..." dialog.
/// </summary>
public class ConfirmFlowerPlantedDialogAction : AbstractAction
{
    public ConfirmFlowerPlantedDialogAction()
    {
    }

    public override sealed async ValueTask RunAsync(IInteractionProvider provider)
    {
        // Click on the "Ok" button.
        // Note: Depending on the name of the flower, the button may be at two different
        // locations (712 and 727). Therefore, we click on the middle of these to hit the
        // button in both cases.
        await MouseHelpers.DoSimpleMouseClickAsync(provider, new Coordinates(800, 720));
    }

    public override string ToString()
    {
        return $"Confirm \"You just planted ...\" dialog";
    }
}
