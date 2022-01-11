using System.Threading.Tasks;

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

    public override sealed async ValueTask RunAsync(IInteractionProvider provider)
    {
        // Click on the "Water" button.
        await MouseHelpers.DoSimpleMouseClickAsync(
            provider,
            new Coordinates(76, 374),
            HorizontalScaleAlignment.Left);
    }

    public override string ToString()
    {
        return $"Water";
    }
}
