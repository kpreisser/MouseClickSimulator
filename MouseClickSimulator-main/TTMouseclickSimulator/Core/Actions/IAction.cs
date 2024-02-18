using System;

using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Actions;

/// <summary>
/// An action is called by the simulator to do something, e.g. press keys or do mouse clicks.
/// Note: A new action will need to be added to the XmlProjectDeserializer so that it is
/// able to deserialize it from an XML file. You also need to ensure that the action only
/// has one constructor.
/// </summary>
public interface IAction
{
    /// <summary>
    /// An event that is rised when the action wants to let subscribers know
    /// that its state has changed. This is useful for the GUI.
    /// </summary>
    event Action<string>? ActionInformationUpdated;

    /// <summary>
    /// Gets the simulator capabilities that are required by this action.
    /// </summary>
    /// <returns></returns>
    SimulatorCapabilities RequiredCapabilities
    {
        get;
    }

    /// <summary>
    /// Runs the action using the specified <see cref="IInteractionProvider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    void Run(IInteractionProvider provider);
}
