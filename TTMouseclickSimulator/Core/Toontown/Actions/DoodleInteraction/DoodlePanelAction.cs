using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.DoodleInteraction;

/// <summary>
/// An action that clicks on the doodle interaction panel (Feed, Scratch, Call)
/// </summary>
public class DoodlePanelAction : AbstractAction
{
    private readonly DoodlePanelButton button;

    public DoodlePanelAction(DoodlePanelButton button)
    {
        this.button = button;
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override void Run(IInteractionProvider provider)
    {
        provider.ThrowIfNotToontownRewritten(nameof(DoodlePanelAction));

        var c = new Coordinates(1397, 206 + (int)this.button * 49);
        MouseHelpers.DoSimpleMouseClick(provider, c, HorizontalScaleAlignment.Right);
    }

    public override string ToString()
    {
        return $"Doodle Panel – Button: {this.button}";
    }

    public enum DoodlePanelButton : int
    {
        Feed = 0,
        Scratch = 1,
        Call = 2
    }
}
