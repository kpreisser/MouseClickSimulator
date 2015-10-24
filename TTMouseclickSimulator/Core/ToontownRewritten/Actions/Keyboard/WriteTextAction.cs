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
    [Serializable]
    public class WriteTextAction : IAction
    {

        private string text;
        private int? pauseTime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="pauseTime">If not null, writes a single character
        /// and then pauses the specified time before writing the next one.
        /// Otherwise, all characters are written immediately.</param>
        public WriteTextAction(string text, int? pauseTime = null)
        {
            this.text = text;
            this.pauseTime = pauseTime;
        }

        public async Task RunAsync(IInteractionProvider provider)
        {
            // write the text and presses enter.
            if (!pauseTime.HasValue)
                provider.WriteText(text);
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    provider.WriteText(text[i].ToString());
                    await provider.WaitAsync(pauseTime.Value);
                }
            }

            // A CR LF (\r\n) in the above string would not have the desired effect;
            // instead we need to press the enter key.
            provider.PressKey(AbstractWindowsEnvironment.VirtualKeyShort.RETURN);
            await provider.WaitAsync(100);
            provider.ReleaseKey(AbstractWindowsEnvironment.VirtualKeyShort.RETURN);
        }
    }
}
