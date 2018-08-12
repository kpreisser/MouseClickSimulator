using System;
using System.Collections.Generic;

namespace TTMouseclickSimulator.Core.Actions
{
    public abstract class AbstractActionContainer : AbstractAction, IActionContainer
    {
        public event Action<int?> SubActionStartedOrStopped;

        public abstract IList<IAction> SubActions { get; }

        protected void OnSubActionStartedOrStopped(int? index)
        {
            SubActionStartedOrStopped?.Invoke(index);
        }            
    }
}
