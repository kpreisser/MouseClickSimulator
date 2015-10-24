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
        private readonly CompoundActionType type;

        private readonly int minimumWaitInterval;
        private readonly int maximumWaitInterval;
        private readonly bool loop;

        private readonly Random rng = new Random();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionList"></param>
        /// <param name="type">Specifies in what order the actions should be run.</param>
        /// <param name="minimumWaitInterval">Specifies the minimum wait time that
        /// should be used after an action has completed</param>
        /// <param name="maximumWaitInterval">Specifies the maximum wait time that
        /// should be used after an action has completed</param>
        /// <param name="loop">If false, the action will return after a complete run. Otherwise
        /// it will loop endlessly. Note that using false is not possible when specifying
        /// CompoundActionType.RandomIndex as type.</param>
        public CompoundAction(IList<IAction> actionList,
            CompoundActionType type = CompoundActionType.Sequential, 
            int minimumWaitInterval = 0, int maximumWaitInterval = 0, bool loop = true)
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
            if (type == CompoundActionType.RandomIndex && !loop)
                throw new ArgumentException("When using CompoundActionType.RandomIndex, it is not possible "
                    + " to disable the loop.");

            this.actionList = actionList;
            this.type = type;
            this.minimumWaitInterval = minimumWaitInterval;
            this.maximumWaitInterval = maximumWaitInterval;
            this.loop = loop;
        }


        public async Task RunAsync(IInteractionProvider provider)
        {
            // Run the actions.
            int currentIdx = -1;
            int[] randomOrder = null;

            Func<int> getNextActionIndex;
            if (type == CompoundActionType.Sequential)
                getNextActionIndex = () => currentIdx = (currentIdx + 1) % actionList.Count;
            else if (type == CompoundActionType.RandomIndex)
                getNextActionIndex = () => rng.Next(actionList.Count);
            else
            {
                randomOrder = new int[actionList.Count];
                getNextActionIndex = () =>
                {
                    currentIdx = (currentIdx + 1) % actionList.Count;
                    if (currentIdx == 0)
                    {
                        // Generate a new order array.
                        for (int i = 0; i < randomOrder.Length; i++)
                            randomOrder[i] = i;
                        for (int i = 0; i < randomOrder.Length; i++)
                        {
                            int rIdx = rng.Next(randomOrder.Length - i);
                            int tmp = randomOrder[i];
                            randomOrder[i] = randomOrder[i + rIdx];
                            randomOrder[i + rIdx] = tmp;
                        }
                    }

                    return randomOrder[currentIdx];
                };
            }

            while (true)
            {
                // Check if the simulator has already been canceled.
                provider.EnsureNotCanceled();
                
                IAction action = actionList[getNextActionIndex()];
                await action.RunAsync(provider);

                // After running an action, wait.
                int waitInterval = rng.Next(minimumWaitInterval, maximumWaitInterval);
                await provider.WaitAsync(waitInterval);
            }
        }


        public enum CompoundActionType : int
        {
            /// <summary>
            /// Specifies that the inner actions should be executed sequentially.
            /// After a run of all actions is complete, either a new run will be started
            /// (if loop is true) or the compound action returns.
            /// </summary>
            Sequential = 0,
            /// <summary>
            /// Specifies that the inner actions should be executed in random order.
            /// This means if n actions are specified and the n-th action has been executed,
            /// every other action before also has been executed once.
            /// After a run of all actions is complete, either a new run will be started
            /// (if loop is true) or the compound action returns.
            /// </summary>
            RandomOrder = 1,
            /// <summary>
            /// Specifies that the inner actions should be executed randomly. That means
            /// that some actions might be executed more often than others and some actions
            /// might never be executed.
            /// When using this type, it is not possible to use loop = false because there is
            /// no terminated run of the actions.
            /// </summary>
            RandomIndex = 2
        }
    }
}
