using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Keyboard;

/// <summary>
/// An action for pressing a key for a specific amount of time.
/// </summary>
public class PressKeyAction : AbstractAction
{
    private readonly AbstractWindowsEnvironment.VirtualKey key;
    private readonly int duration;

    public PressKeyAction(AbstractWindowsEnvironment.VirtualKey key, int duration)
    {
        this.key = key;
        this.duration = duration;
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.KeyboardInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        provider.PressKey(this.key);

        // Use a accurate timer for measuring the time after we need to release the key.
        provider.Wait(this.duration, true);
        provider.ReleaseKey(this.key);
    }

    public override string ToString()
    {
        return $"Press Key – Key: {this.key}, Duration: {this.duration}";
    }
}
