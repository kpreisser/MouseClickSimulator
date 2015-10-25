using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Actions
{
    public abstract class AbstractActionContainer : AbstractAction, IActionContainer
    {
        public abstract IList<IAction> SubActions { get; }

        public event Action<int?> SubActionStartedOrStopped;

        protected void OnSubActionStartedOrStopped(int? index)
        {
            if (SubActionStartedOrStopped != null)
                SubActionStartedOrStopped(index);
        }
    }
}
