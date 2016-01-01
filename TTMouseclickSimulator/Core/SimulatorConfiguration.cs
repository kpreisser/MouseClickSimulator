using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;

namespace TTMouseclickSimulator.Core
{
    public class SimulatorConfiguration
    {
        /// <summary>
        /// Specifies the action that is run by the simulator.
        /// </summary>
        public IAction MainAction { get; set; }
    }
}
