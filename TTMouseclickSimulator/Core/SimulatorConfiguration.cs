using System.Collections.Generic;

using TTMouseclickSimulator.Core.Actions;

namespace TTMouseclickSimulator.Core
{
    public class SimulatorConfiguration
    {
        /// <summary>
        /// Specifies the action that is run by the simulator.
        /// </summary>
        public IAction MainAction { get; set; }

        public List<QuickActionDescriptor> QuickActions { get; } = new List<QuickActionDescriptor>(2);

        
        public class QuickActionDescriptor
        {
            public string Name { get; set; }

            public IAction Action { get; set; }
        }
    }
}
