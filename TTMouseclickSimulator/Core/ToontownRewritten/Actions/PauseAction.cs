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
    [Serializable]
    public class PauseAction : AbstractAction
    {
        private readonly int duration;

        public PauseAction(int duration)
        {
            this.duration = duration;
        }

        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            await provider.WaitAsync(duration);
        }


        public override string ToString()
        {
            return $"Pause – Duration: {duration}";
        }
    }
}
