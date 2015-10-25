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
    public class LoopAction : AbstractActionContainer
    {

        private readonly IAction action;
        /// <summary>
        /// Specifies how often the action is run. null means infinite.
        /// </summary>
        private readonly int? count;

        public LoopAction(IAction action, int? count = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (count.HasValue && count.Value < 0)
                throw new ArgumentException("count must not be negative");

            this.action = action;
            this.count = count;
        }

        public override IList<IAction> SubActions
        {
            get { return new List<IAction>() { action }; }
        }

        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            OnSubActionStartedOrStopped(0);
            try
            {
                for (int i = 0; !count.HasValue || i < count.Value; i++)
                {
                    provider.EnsureNotCanceled();
                    OnActionInformationUpdated($"Iteration {i + 1}/{count}");
                    await action.RunAsync(provider);
                }
            }
            finally
            {
                OnSubActionStartedOrStopped(null);
            }
        }


        public override string ToString()
        {
            return $"Loop – Count: {count}";
        }
    }
}
