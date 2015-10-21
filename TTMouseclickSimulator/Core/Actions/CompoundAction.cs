using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions
{
    /// <summary>
    /// An action that loops through a list of other actions either in order or by chance.
    /// </summary>
    public class CompoundAction : IAction
    {

        public const int WaitIntervalMinimum = 0;
        public const int WaitIntervalMaximum = 60000;


        private readonly IList<IAction> actionList;
        private readonly bool runInOrder;

        private readonly int minimumWaitInterval;
        private readonly int maximumWaitInterval;

        private readonly Random rng = new Random();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionList"></param>
        /// <param name="runInOrder">Specifies if the actions should be run in the order 
        /// as they are specified (true) or in random order (false). The default is true.</param>
        /// <param name="minimumWaitInterval">Specifies the minimum wait time that
        /// should be used after an action has completed</param>
        /// <param name="maximumWaitInterval">Specifies the maximum wait time that
        /// should be used after an action has completed</param>
        public CompoundAction(IList<IAction> actionList, bool runInOrder, 
            int minimumWaitInterval, int maximumWaitInterval)
        {

            if (actionList == null || actionList.Count == 0)
                throw new ArgumentException("There must be at least one IAction to start the simulator.");
            if (minimumWaitInterval < WaitIntervalMinimum
                    || minimumWaitInterval > WaitIntervalMaximum
                    || maximumWaitInterval < WaitIntervalMinimum
                    || maximumWaitInterval > WaitIntervalMaximum)
                throw new ArgumentOutOfRangeException("The wait interval values must be between " +
                    $"{WaitIntervalMinimum} and {WaitIntervalMaximum} milliseconds.");
            if (minimumWaitInterval > maximumWaitInterval)
                throw new ArgumentException("The minimum wait interval must not be greater "
                    + "than the maximum wait interval.");

            this.actionList = actionList;
            this.runInOrder = runInOrder;
            this.minimumWaitInterval = minimumWaitInterval;
            this.maximumWaitInterval = maximumWaitInterval;
        }


        public async Task RunAsync(IInteractionProvider provider)
        {
            // Run the actions.
            int nextActionIdx = 0;

            while (true)
            {
                // Check if the simulator has already been canceled.
                provider.EnsureNotCanceled();

                if (runInOrder)
                    nextActionIdx = (nextActionIdx + 1) % actionList.Count;
                else
                    nextActionIdx = rng.Next(actionList.Count);

                IAction action = actionList[nextActionIdx];

                await action.RunAsync(provider);

                // After running an action, wait.
                int waitInterval = rng.Next(minimumWaitInterval, maximumWaitInterval);
                await provider.WaitAsync(waitInterval);
            }
        }
    }
}
