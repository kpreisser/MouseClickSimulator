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
        private readonly AbstractEnvironmentInterface environmentInterface;

        private readonly StandardInteractionProvider provider;
        private readonly Random rng;

        private bool canceled = false;

        public Simulator(SimulatorConfiguration config, AbstractEnvironmentInterface environmentInterface)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (environmentInterface == null)
                throw new ArgumentNullException(nameof(environmentInterface));

            this.config = config;
            this.environmentInterface = environmentInterface;

            provider = new StandardInteractionProvider(environmentInterface);

        }

        public async Task RunAsync()
        {
            if (canceled)
                throw new InvalidOperationException("The simulator has already been canceled.");

            // Run the actions.
            int nextActionIdx = 0;

            try {
                while (true)
                {
                    if (config.RunInOrder)
                    {
                        nextActionIdx = (nextActionIdx + 1) % config.Actions.Count;
                    }
                    else
                    {
                        nextActionIdx = rng.Next(config.Actions.Count);
                    }

                    IAction action = config.Actions[nextActionIdx];
                    await action.RunAsync(provider);

                    // After running an action, wait.
                    int waitInterval = rng.Next(config.MinimumWaitInterval, config.MaximumWaitInterval);
                    await provider.WaitAsync(waitInterval);

                }
            }
            catch (ActionCanceledException)
            {
                // TODO: Call some event so the GUI knows that we have stopped
            }
        }

        public void Cancel()
        {
            provider.Dispose();
            canceled = true;
        }

    }
}
