using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core
{
    public class Simulator
    {
        
        private readonly SimulatorConfiguration config;
        private readonly AbstractWindowsEnvironment environmentInterface;

        private readonly StandardInteractionProvider provider;
        private readonly Action cancelCallback;

        private volatile bool canceled = false;


        public event Action SimulatorStarted;
        // TODO: This needs a refactoring so that an action can update its state.
        //public event Action<IAction, int> ActionStarted;
        public event Action SimulatorStopped;



        public Simulator(SimulatorConfiguration config, AbstractWindowsEnvironment environmentInterface)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (environmentInterface == null)
                throw new ArgumentNullException(nameof(environmentInterface));
            if (config.Action == null)
                throw new ArgumentException("There must be an action specified in the SimulatorConfiguration.");
            

            this.config = config;
            this.environmentInterface = environmentInterface;

            provider = new StandardInteractionProvider(environmentInterface, out cancelCallback);
            

        }

        /// <summary>
        /// Asynchronously runs this simulator.
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            if (canceled)
                throw new InvalidOperationException("The simulator has already been canceled.");

            try
            {
                using (provider)
                {
                    OnSimulatorStarted();

                    provider.Initialize();

                    // Wait a bit so that the window can go into foreground.
                    await provider.WaitAsync(1000);

                    // Run the action.
                    await config.Action.RunAsync(provider);

                    // Normally the main action would be a CompoundAction that never returns, but
                    // it is possible that the action will return normally.
                }

            }
            finally
            {
                canceled = true;
                OnSimulatorStopped();
            }
        }

        /// <summary>
        /// Cancels the simulator. This method can be called from the GUI thread while
        /// the task that runs RunAsync is still active. It can also be called from
        /// another thread.
        /// </summary>
        public void Cancel()
        {
            canceled = true;
            cancelCallback();
        }


        protected void OnSimulatorStarted()
        {
            if (SimulatorStarted != null)
                SimulatorStarted();
        }

        //protected void OnActionStarted(IAction simAction, int idx)
        //{
        //    if (ActionStarted != null)
        //        ActionStarted(simAction, idx);
        //}

        protected void OnSimulatorStopped()
        {
            if (SimulatorStopped != null)
            {
                SimulatorStopped();
            }
        }

    }
}
