using System;
using System.Collections.Generic;

namespace TTMouseClickSimulator.Core.Actions;

public abstract class AbstractActionContainer : AbstractAction, IActionContainer
{
    public event Action<int?>? SubActionStartedOrStopped;

    public abstract IReadOnlyList<IAction> SubActions { get; }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get
        {
            var capabilities = default(SimulatorCapabilities);

            foreach (var subAction in this.SubActions ?? Array.Empty<IAction>())
                capabilities |= subAction.RequiredCapabilities;

            return capabilities;
        }
    }

    protected void OnSubActionStartedOrStopped(int? index)
    {
        SubActionStartedOrStopped?.Invoke(index);
    }
}
