using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Keyboard
{
    /// <summary>
    /// An action that writes the given string and a line break into the window.
    /// </summary>
    public class WriteTextAction : AbstractAction
    {

        private string text;
        private int? pauseDuration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="pauseDuration">If not null, writes a single character
        /// and then pauses the specified time before writing the next one.
        /// Otherwise, all characters are written immediately.</param>
        public WriteTextAction(string text, int? pauseDuration = null)
        {
            this.text = text;
            this.pauseDuration = pauseDuration;
        }

        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            // write the text and presses enter.
            if (!pauseDuration.HasValue)
                provider.WriteText(text);
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    provider.WriteText(text[i].ToString());
                    await provider.WaitAsync(pauseDuration.Value);
                }
            }

            // A CR LF (\r\n) in the above string would not have the desired effect;
            // instead we need to press the enter key.
            provider.PressKey(AbstractWindowsEnvironment.VirtualKeyShort.Enter);
            await provider.WaitAsync(100);
            provider.ReleaseKey(AbstractWindowsEnvironment.VirtualKeyShort.Enter);
        }


        public override string ToString()
        {
            return $"Write Text – Text: \"{text}\"" + (pauseDuration.HasValue ? "" : $", Pause Duration: {pauseDuration}");
        }
    }
}
