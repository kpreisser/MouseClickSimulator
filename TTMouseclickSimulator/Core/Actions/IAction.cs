using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Actions
{
    /// <summary>
    /// An action is called by the simulator to do something, e.g. press keys or do mouse clicks.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Asynchonously runs the action using the specified IInteractionProvider.
        /// </summary>
        /// <param name="waitable"></param>
        /// <returns></returns>
        Task RunAsync(IInteractionProvider provider);
    }
}
