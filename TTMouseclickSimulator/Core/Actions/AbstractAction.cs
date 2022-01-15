﻿using System;
using System.Threading.Tasks;

using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions;

public abstract class AbstractAction : IAction
{
    public event Action<string>? ActionInformationUpdated;

    public abstract SimulatorCapabilities RequiredCapabilities
    {
        get;
    }

    public abstract ValueTask RunAsync(IInteractionProvider provider);

    protected void OnActionInformationUpdated(string text)
    {
        ActionInformationUpdated?.Invoke(text);
    }
}
