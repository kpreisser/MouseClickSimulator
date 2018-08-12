using System;
using System.Threading.Tasks;

using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions
{
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
        event Action<string> ActionInformationUpdated;

        /// <summary>
        /// Asynchonously runs the action using the specified IInteractionProvider.
        /// </summary>
        /// <param name="waitable"></param>
        /// <returns></returns>
        Task RunAsync(IInteractionProvider provider);        
    }
}
