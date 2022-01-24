using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Keyboard;

/// <summary>
/// An action for releasing a key, which was previousley depressed with the
/// <see cref="KeyDownAction"/>.
/// </summary>
public class KeyUpAction : AbstractAction
{
    private readonly WindowsEnvironment.VirtualKey key;

    public KeyUpAction(WindowsEnvironment.VirtualKey key)
    {
        this.key = key;
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.KeyboardInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        provider.ReleaseKey(this.key);
    }

    public override string ToString()
    {
        return $"Key Up – Key: {this.key}";
    }
}
