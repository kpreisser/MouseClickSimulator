using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions
{
    /// <summary>
    /// An action that loops through a list of other actions either in order or by chance.
    /// </summary>
    public class CompoundAction : AbstractActionContainer
    {
        public const int PauseIntervalMinimum = 0;
        public const int PauseIntervalMaximum = 60000;


        private readonly IList<IAction> actionList;
        private readonly CompoundActionType type;

        private readonly int minimumPauseDuration;
        private readonly int maximumPauseDuration;
        private readonly bool loop;

        private readonly Random rng = new Random();

        public override sealed IList<IAction> SubActions => this.actionList;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionList"></param>
        /// <param name="type">Specifies in what order the actions should be run.</param>
        /// <param name="minimumPause">Specifies the minimum wait time that
        /// should be used after an action has completed</param>
        /// <param name="maximumPause">Specifies the maximum wait time that
        /// should be used after an action has completed</param>
        /// <param name="loop">If false, the action will return after a complete run. Otherwise
        /// it will loop endlessly. Note that using false is not possible when specifying
        /// CompoundActionType.RandomIndex as type.</param>
        public CompoundAction(IList<IAction> actionList,
            CompoundActionType type = CompoundActionType.Sequential, 
            int minimumPause = 0, int maximumPause = 0, bool loop = true)
        {

            if (actionList == null || actionList.Count == 0)
                throw new ArgumentException("There must be at least one IAction to start the simulator.");
            if (minimumPause < PauseIntervalMinimum
                    || minimumPause > PauseIntervalMaximum
                    || maximumPause < PauseIntervalMinimum
                    || maximumPause > PauseIntervalMaximum)
                throw new ArgumentOutOfRangeException("The pause duration values must be between " +
                    $"{PauseIntervalMinimum} and {PauseIntervalMaximum} milliseconds.");
            if (minimumPause > maximumPause)
                throw new ArgumentException("The minimum pause duration must not be greater "
                    + "than the maximum wait interval.");
            if (type == CompoundActionType.RandomIndex && !loop)
                throw new ArgumentException("When using CompoundActionType.RandomIndex, it is not possible "
                    + " to disable the loop.");

            this.actionList = actionList;
            this.type = type;
            this.minimumPauseDuration = minimumPause;
            this.maximumPauseDuration = maximumPause;
            this.loop = loop;
        }


        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            // Run the actions.
            int currentIdx = -1;
            int[] randomOrder = null;

            Func<int> getNextActionIndex;
            if (this.type == CompoundActionType.Sequential)
                getNextActionIndex = () => 
                    (!this.loop && currentIdx + 1 == this.actionList.Count) ? -1 
                    : currentIdx = (currentIdx + 1) % this.actionList.Count;
            else if (this.type == CompoundActionType.RandomIndex)
                getNextActionIndex = () => this.rng.Next(this.actionList.Count);
            else
            {
                randomOrder = new int[this.actionList.Count];
                getNextActionIndex = () =>
                {
                    if (!this.loop && currentIdx + 1 == this.actionList.Count)
                        return -1;

                    currentIdx = (currentIdx + 1) % this.actionList.Count;
                    if (currentIdx == 0)
                    {
                        // Generate a new order array.
                        for (int i = 0; i < randomOrder.Length; i++)
                            randomOrder[i] = i;
                        for (int i = 0; i < randomOrder.Length; i++)
                        {
                            int rIdx = this.rng.Next(randomOrder.Length - i);
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
                int nextIdx = getNextActionIndex();
                if (nextIdx == -1)
                    break;

                OnActionInformationUpdated($"Running action {nextIdx + 1}");

                for (;;)
                {
                    try
                    {
                        // Check if the simulator has already been canceled.
                        provider.EnsureNotCanceled();

                        OnSubActionStartedOrStopped(nextIdx);
                        try
                        {
                            var action = this.actionList[nextIdx];
                            await action.RunAsync(provider);
                        }
                        finally
                        {
                            OnSubActionStartedOrStopped(null);
                        }

                        // After running an action, wait.
                        int waitInterval = this.rng.Next(this.minimumPauseDuration, this.maximumPauseDuration);
                        OnActionInformationUpdated($"Pausing {waitInterval} ms");

                        await provider.WaitAsync(waitInterval);
                    }
                    catch (Exception ex) when (!(ex is SimulatorCanceledException))
                    {
                        await provider.CheckRetryForExceptionAsync(ExceptionDispatchInfo.Capture(ex));
                        continue;
                    }
                    break;
                }
            }
        }


        public override string ToString() => $"Compound – Type: {this.type}, " 
            + $"MinPause: {this.minimumPauseDuration}, MaxPause: {this.maximumPauseDuration}, Loop: {this.loop}";


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
