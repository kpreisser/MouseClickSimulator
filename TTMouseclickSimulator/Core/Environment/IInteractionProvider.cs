using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Environment
{
    /// <summary>
    /// Allows actions to interact with the destination window, e.g. press keys and
    /// simulate mouse clicks and to wait asynchronously.
    /// </summary>
    public interface IInteractionProvider
    {

        /// <summary>
        /// Checks that the InteractionProvider has not been canceled.
        /// </summary>
        void EnsureNotCanceled();

        /// <summary>
        /// Asynchronously waits until the specified interval is elapsed or an exception is thrown.
        /// </summary>
        /// <param name="interval">The interval to wait.</param>
        /// <returns></returns>
        /// <exception cref="SimulatorCanceledException">If the wait has been cancelled.
        /// IActions don't need to catch this exception.</exception>
        Task WaitAsync(int millisecondsTimeout);

        /// <summary>
        /// Gets the current position of the destination window.
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="SimulatorCanceledException">If the simulator has been cancelled.
        /// IActions don't need to catch this exception.</exception>
        WindowPosition GetCurrentWindowPosition();

        IScreenshotContent CreateCurrentWindowScreenshot();

        void MoveMouse(int x, int y);

        void MoveMouse(Coordinates c);

        void PressMouseButton();

        void ReleaseMouseButton();

        void PressKey(AbstractWindowsEnvironment.VirtualKeyShort key);
        void ReleaseKey(AbstractWindowsEnvironment.VirtualKeyShort key);

    }


    /// <summary>
    /// Thrown when an action has been canceled because the simulator has been stopped.
    /// </summary>
    public class SimulatorCanceledException : Exception
    {
        public SimulatorCanceledException() : 
            base("The Simulator has been canceled.")
        {

        }
    }
}
