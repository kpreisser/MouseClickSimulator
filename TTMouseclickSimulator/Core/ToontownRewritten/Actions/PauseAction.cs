using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions
{
    /// <summary>
    /// An action that just waits using the specified amount of time.
    /// </summary>
    public class PauseAction : IAction
    {
        private readonly int timeout;

        public PauseAction(int timeout)
        {
            this.timeout = timeout;
        }

        public async Task RunAsync(IInteractionProvider provider)
        {
            await provider.WaitAsync(timeout);
        }
    }
}
