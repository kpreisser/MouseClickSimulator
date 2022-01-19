using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Gardening;

/// <summary>
/// An action which waters a flower or tree.
/// </summary>
public class WaterAction : AbstractAction
{
    public WaterAction()
    {
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        // Click on the "Water" button.
        MouseHelpers.DoSimpleMouseClick(
            provider,
            new Coordinates(76, 374),
            HorizontalScaleAlignment.Left);
    }

    public override string ToString()
    {
        return $"Water";
    }
}
