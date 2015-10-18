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

        private readonly AbstractEnvironmentInterface.VirtualKeyShort keyCode;
        private readonly int timeot;

        public PressKeyAction(AbstractEnvironmentInterface.VirtualKeyShort keyCode, int timeout)
        {
            this.keyCode = keyCode;
            this.timeot = timeout;
        }


        public async Task RunAsync(IInteractionProvider provider)
        {
            provider.PressKey(keyCode);
            await provider.WaitAsync(timeot);
            provider.ReleaseKey(keyCode);
        }
    }
}
