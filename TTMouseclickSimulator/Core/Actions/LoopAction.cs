using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions
{
    /// <summary>
    /// Represents an action that runs another action in a loop.
    /// </summary>
    public class LoopAction : IAction
    {

        private readonly IAction action;
        /// <summary>
        /// Specifies how often the action is run. null means infinite.
        /// </summary>
        private readonly int? counter;

        public LoopAction(IAction action, int? counter)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (counter.HasValue && counter.Value < 0)
                throw new ArgumentException("Counter must not be negative");

            this.action = action;
            this.counter = counter;
        }

        public async Task RunAsync(IInteractionProvider provider)
        {
            for (int i = 0; !counter.HasValue || i < counter.Value; i++)
            {
                provider.EnsureNotCanceled();
                await action.RunAsync(provider);
            }
        }
    }
}
