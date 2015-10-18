using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions
{
    /// <summary>
    /// Allows actions to interact with the destination window, e.g. press keys and
    /// simulate mouse clicks. It also allows to asynchronously wait, using the specified
    /// IWaitable.
    /// </summary>
    public interface IInteractionProvider
    {

        /// <summary>
        /// Asynchronously waits until the specified interval is elapsed or an exception is thrown.
        /// </summary>
        /// <param name="interval">The interval to wait.</param>
        /// <returns></returns>
        /// <exception cref="ActionCanceledException">If the wait has been cancelled.
        /// IActions don't need to catch this exception.</exception>
        Task WaitAsync(int interval);

        /// <summary>
        /// Gets the current position of the destination window.
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ActionCanceledException">If the simulator has been cancelled.
        /// IActions don't need to catch this exception.</exception>
        WindowPosition GetCurrentWindowPosition();

        AbstractEnvironmentInterface.ScreenshotContent CreateCurrentWindowScreenshot();

        void MoveMouse(int x, int y);

        void PressMouseButton();

        void ReleaseMouseButton();

        void PressKey(AbstractEnvironmentInterface.VirtualKeyShort key);
        void ReleaseKey(AbstractEnvironmentInterface.VirtualKeyShort key);

    }


    /// <summary>
    /// Thrown when an action has been canceled because the simulator has been stopped.
    /// </summary>
    public class ActionCanceledException : Exception
    {
        public ActionCanceledException() : 
            base("The Simulator has been canceled.")
        {

        }
    }
}
