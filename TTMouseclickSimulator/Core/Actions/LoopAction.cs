using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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


        public override IList<IAction> SubActions => new List<IAction>() { this.action };


        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            OnSubActionStartedOrStopped(0);
            try
            {
                for (int i = 0; !this.count.HasValue || i < this.count.Value; i++)
                {
                    while (true)
                    {
                        try
                        {
                            provider.EnsureNotCanceled();
                            OnActionInformationUpdated($"Iteration {i + 1}/{this.count?.ToString() ?? "∞"}");
                            await this.action.RunAsync(provider);
                        }
                        catch (Exception ex) when (!(ex is SimulatorCanceledException))
                        {
                            await provider.CheckRetryForExceptionAsync(ex);
                            continue;
                        }
                        break;
                    }
                }
            }
            finally
            {
                OnSubActionStartedOrStopped(null);
            }
        }

        public override string ToString()
        {
            return $"Loop – Count: {this.count?.ToString() ?? "∞"}";
        }
    }
}
