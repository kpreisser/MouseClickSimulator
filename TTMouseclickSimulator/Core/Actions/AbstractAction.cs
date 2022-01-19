using System;

using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions;

public abstract class AbstractAction : IAction
{
    public event Action<string>? ActionInformationUpdated;

    public abstract SimulatorCapabilities RequiredCapabilities
    {
        get;
    }

    public abstract void Run(IInteractionProvider provider);

    protected void OnActionInformationUpdated(string text)
    {
        ActionInformationUpdated?.Invoke(text);
    }
}
