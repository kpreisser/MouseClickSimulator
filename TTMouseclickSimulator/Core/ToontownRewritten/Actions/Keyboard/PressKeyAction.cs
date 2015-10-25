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
    /// An action for pressing a key for a specific amount of time.
    /// </summary>
    public class PressKeyAction : IAction
    {

        private readonly AbstractWindowsEnvironment.VirtualKeyShort key;
        private readonly int duration;

        public PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort key, int duration)
        {
            this.key = key;
            this.duration = duration;
        }


        public async Task RunAsync(IInteractionProvider provider)
        {
            provider.PressKey(key);
            await provider.WaitAsync(duration);
            provider.ReleaseKey(key);
        }
    }
}
