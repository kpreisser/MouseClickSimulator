using System.Collections.Generic;

using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Toontown;

namespace TTMouseClickSimulator.Core;

public class SimulatorConfiguration
{
    public ToontownFlavor ToontownFlavor
    {
        get;
        set;
    }

    /// <summary>
    /// Specifies the action that is run by the simulator.
    /// </summary>
    public IAction? MainAction
    {
        get;
        set;
    }

    public List<QuickActionDescriptor> QuickActions
    {
        get;
    } = new List<QuickActionDescriptor>(2);

    public class QuickActionDescriptor
    {
        public string? Name { get; set; }

        public IAction? Action { get; set; }
    }
}
