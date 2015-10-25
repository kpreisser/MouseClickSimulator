using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions
{
    public abstract class AbstractAction : IAction
    {

        public event Action<string> ActionInformationUpdated;
        
        public abstract Task RunAsync(IInteractionProvider provider);


        protected void OnActionInformationUpdated(string text)
        {
            if (ActionInformationUpdated != null)
                ActionInformationUpdated(text);
        }
    }
}
