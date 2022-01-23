using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Gardening;

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
        provider.ThrowIfNotToontownRewritten(nameof(WaterAction));

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
