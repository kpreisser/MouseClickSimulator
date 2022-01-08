using System;
using System.Collections.Generic;

namespace TTMouseclickSimulator.Core.Actions;

public interface IActionContainer : IAction
{
    /// <summary>
    /// An event that is raised when a subaction has been started
    /// or stopped.
    /// </summary>
    event Action<int?>? SubActionStartedOrStopped;

    IList<IAction> SubActions { get; }
}
