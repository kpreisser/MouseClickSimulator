using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core
{
    public class Simulator
    {
        private readonly IAction mainAction;
        private readonly AbstractWindowsEnvironment environmentInterface;

        private readonly StandardInteractionProvider provider;
        private readonly Action cancelCallback;

        private volatile bool canceled = false;

        public event Action SimulatorStarted;
        public event Action SimulatorStopped;
        public event Action<bool?> SimulatorInitializing;

        /// <summary>
        /// When an exception (which is not a <see cref="SimulatorCanceledException"/>) occurs while an action runs,
        /// this allows the action to check if it should retry or cancel the simulator (in that case, it should
        /// throw an <see cref="SimulatorCanceledException"/>).
        /// </summary>
        public Func<Exception, Task<bool>> AsyncRetryHandler;
        

        public Simulator(IAction mainAction, AbstractWindowsEnvironment environmentInterface)
        {
            if (mainAction == null)
                throw new ArgumentNullException(nameof(mainAction));
            if (environmentInterface == null)
                throw new ArgumentNullException(nameof(environmentInterface));

            this.mainAction = mainAction;
            this.environmentInterface = environmentInterface;

            this.provider = new StandardInteractionProvider(this, environmentInterface, out this.cancelCallback);
        }

        /// <summary>
        /// Asynchronously runs this simulator.
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            if (this.canceled)
                throw new InvalidOperationException("The simulator has already been canceled.");

            try
            {
                using (this.provider)
                {
                    OnSimulatorStarted();

                    // InitializeAsync() does not need to be in the try block because it has its own.
                    await this.provider.InitializeAsync();

                    while (true)
                    {
                        try
                        {
                            // Run the action.
                            await this.mainAction.RunAsync(this.provider);

                            // Normally the main action would be a CompoundAction that never returns, but
                            // it is possible that the action will return normally.
                        }
                        catch (Exception ex) when (!(ex is SimulatorCanceledException))
                        {
                            await this.provider.CheckRetryForExceptionAsync(ex);
                            continue;
                        }
                        break;
                    }
                }
            }
            finally
            {
                this.canceled = true;
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
            this.canceled = true;
            this.cancelCallback();
        }

        protected void OnSimulatorStarted()
        {
            SimulatorStarted?.Invoke();
        }

        protected void OnSimulatorStopped()
        {
            SimulatorStopped?.Invoke();
        }

        internal protected void OnSimulatorInitializing(bool? multipleWindowsAvailable)
        {
            SimulatorInitializing?.Invoke(multipleWindowsAvailable);
        }
    }
}
