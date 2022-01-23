using System;

using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Keyboard;

/// <summary>
/// An action that writes the given string and a line break into the window.
/// </summary>
public class WriteTextAction : AbstractAction
{
    private readonly string text;
    private readonly int? pauseDuration;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="pauseDuration">If not null, writes a single character
    /// and then pauses the specified time before writing the next one.
    /// Otherwise, all characters are written immediately.</param>
    public WriteTextAction(string text, int? pauseDuration = null)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        this.text = text;
        this.pauseDuration = pauseDuration;
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.KeyboardInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        // write the text and presses enter.
        if (!this.pauseDuration.HasValue)
        {
            provider.WriteText(this.text);
        }
        else
        {
            for (int i = 0; i < this.text.Length; i++)
            {
                provider.WriteText(this.text[i].ToString());
                provider.Wait(this.pauseDuration.Value);
            }
        }

        // A CR LF (\r\n) in the above string would not have the desired effect;
        // instead we need to press the enter key.
        provider.PressKey(WindowsEnvironment.VirtualKey.Enter);
        provider.Wait(100);
        provider.ReleaseKey(WindowsEnvironment.VirtualKey.Enter);
    }

    public override string ToString()
    {
        return $"Write Text – Text: \"{this.text}\"" +
            (this.pauseDuration.HasValue ? "" : $", Pause Duration: {this.pauseDuration}");
    }
}
