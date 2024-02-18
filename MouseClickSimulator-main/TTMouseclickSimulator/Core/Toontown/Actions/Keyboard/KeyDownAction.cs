using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Keyboard;

/// <summary>
/// An action for depressing a key, which can later be released with the
/// <see cref="KeyUpAction"/>.
/// </summary>
public class KeyDownAction : AbstractAction
{
    private readonly WindowsEnvironment.VirtualKey key;

    public KeyDownAction(WindowsEnvironment.VirtualKey key)
    {
        this.key = key;
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.KeyboardInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        provider.PressKey(this.key);
    }

    public override string ToString()
    {
        return $"Key Down – Key: {this.key}";
    }
}
