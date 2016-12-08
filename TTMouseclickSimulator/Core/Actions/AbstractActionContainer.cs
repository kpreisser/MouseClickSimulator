using System;
using System.Collections.Generic;

namespace TTMouseclickSimulator.Core.Actions
{
    public abstract class AbstractActionContainer : AbstractAction, IActionContainer
    {
        public abstract IList<IAction> SubActions { get; }

        public event Action<int?> SubActionStartedOrStopped;


        protected void OnSubActionStartedOrStopped(int? index) =>
            SubActionStartedOrStopped?.Invoke(index);
    }
}
