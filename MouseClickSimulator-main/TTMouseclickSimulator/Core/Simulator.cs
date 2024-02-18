using System;

using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;
using TTMouseClickSimulator.Core.Toontown;
using TTMouseClickSimulator.Core.Toontown.Environment;

namespace TTMouseClickSimulator.Core;

public class Simulator : IDisposable
{
    private readonly IAction mainAction;

    private readonly InteractionProvider provider;

    public event Action? SimulatorStarted;
    public event Action? SimulatorStopped;
    public event Action<bool?>? SimulatorInitializing;

    public Simulator(
        ToontownFlavor toontownFlavor,
        IAction mainAction,
        WindowsEnvironment environmentInterface,
        bool useWasdMovement,
        bool backgroundMode)
    {
        if (mainAction is null)
            throw new ArgumentNullException(nameof(mainAction));
        if (environmentInterface is null)
            throw new ArgumentNullException(nameof(environmentInterface));

        this.mainAction = mainAction;
        this.RequiredCapabilities = mainAction.RequiredCapabilities;

        // TODO: The simulator shouldn't have to know about this TT-specific subclass.
        this.provider = new ToontownInteractionProvider(
            this,
            toontownFlavor,
            environmentInterface,
            backgroundMode)
        {
            UseWasdMovement = useWasdMovement
        };
    }

    public SimulatorCapabilities RequiredCapabilities
    {
        get;
    }

    /// <summary>
    /// When an exception (which is not a <see cref="OperationCanceledException"/>) occurs
    /// while an action runs, this allows the action to check if it should retry or cancel
    /// the simulator (in that case, it should throw an
    /// <see cref="OperationCanceledException"/>).
    /// </summary>
    public Func<Exception, bool>? RetryHandler
    {
        get;
        set;
    }

    public bool IsCancelled
    {
        get => this.provider.CancellationToken.IsCancellationRequested;
    }

    /// <summary>
    /// Runs this simulator.
    /// </summary>
    /// <returns></returns>
    public void Run()
    {
        if (this.IsCancelled)
        {
            throw new InvalidOperationException(
                "The simulator has already been canceled or stopped.");
        }

        try
        {
            this.OnSimulatorStarted();

            // Initialize() does not need to be in the try-catch block because it has
            // its own.
            this.provider.Initialize();

            while (true)
            {
                try
                {
                    // Run the action.
                    this.mainAction.Run(this.provider);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    this.provider.CheckRetryForException(ex);
                    continue;
                }

                break;
            }
        }
        finally
        {
            // Ensure we are marked as cancelled (so Run() cannot be called again).
            this.provider.Cancel();

            // Cancel the interactions here because we don't want the GUI thread to
            // have to do this when disposing of the simulator later.
            this.provider.CancelActiveInteractions();
            this.OnSimulatorStopped();
        }
    }

    /// <summary>
    /// Cancels the simulator.
    /// </summary>
    /// <remarks>
    /// This method can be called from another thread while the thread that runs
    /// <see cref="Run"/> is still active.
    /// </remarks>
    public void Cancel()
    {
        this.provider.Cancel();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal protected void OnSimulatorInitializing(bool? multipleWindowsAvailable)
    {
        this.SimulatorInitializing?.Invoke(multipleWindowsAvailable);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.provider.Dispose();
        }
    }

    protected void OnSimulatorStarted()
    {
        this.SimulatorStarted?.Invoke();
    }

    protected void OnSimulatorStopped()
    {
        this.SimulatorStopped?.Invoke();
    }
}
