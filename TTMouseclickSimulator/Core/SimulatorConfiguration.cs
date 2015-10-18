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
        private IList<IAction> actions;

        private bool runInOrder = true;

        private int maximumWaitInterval;
        private int minimumWaitInterval;

        /// <summary>
        /// Specifies the list of actions that the simulator should run.
        /// </summary>
        public IList<IAction> Actions
        {
            get
            {
                return actions;
            }

            set
            {
                actions = value;
            }
        }

        /// <summary>
        /// Specifies if the actions should be run in the order as they are specified (true) or
        /// in random order (false). The default is true.
        /// </summary>
        public bool RunInOrder
        {
            get
            {
                return runInOrder;
            }

            set
            {
                runInOrder = value;
            }
        }

        /// <summary>
        /// Specifies the minimum wait time that should be used after an action has completed.
        /// </summary>
        public int MinimumWaitInterval
        {
            get
            {
                return minimumWaitInterval;
            }

            set
            {
                minimumWaitInterval = value;
            }
        }

        /// <summary>
        /// Specifies the maximum wait time that should be used after an action has completed.
        /// </summary>
        public int MaximumWaitInterval
        {
            get
            {
                return maximumWaitInterval;
            }

            set
            {
                maximumWaitInterval = value;
            }
        }
    }
}
