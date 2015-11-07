using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Environment
{
    internal class StandardInteractionProvider : IInteractionProvider, IDisposable
    {

        private bool disposed = false;
        /// <summary>
        /// Specifies if this InteractionProvider has been canceled. This flag can be set by
        /// another thread while the simulator is running.
        /// </summary>
        private volatile bool canceled = false;

        private readonly SemaphoreSlim waitSemaphore = new SemaphoreSlim(0);

        private readonly AbstractWindowsEnvironment environmentInterface;

        private Process process;
        private bool isMouseButtonPressed = false;
        private List<AbstractWindowsEnvironment.VirtualKeyShort> keysCurrentlyPressed 
            = new List<AbstractWindowsEnvironment.VirtualKeyShort>();


        public StandardInteractionProvider(AbstractWindowsEnvironment environmentInterface,
            out Action cancelCallback)
        {
            this.environmentInterface = environmentInterface;
            cancelCallback = HandleCancelCallback;
        }


        public void Initialize()
        {
            process = environmentInterface.FindProcess();

            // Bring the destination window to foreground.
            IntPtr hWnd = environmentInterface.FindMainWindowHandleOfProcess(process);
            environmentInterface.BringWindowToForeground(hWnd);
        }

        private void HandleCancelCallback()
        {
            canceled = true;
            // Release the semaphore (so that a task that is waiting can continue).
            try
            {
                waitSemaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // Can happen when another thread tries to cancel the simulator while the task
                // that runs the Simulator.RunAsync() method has already disposed the
                // StandardInteractionProvider.
                // In that case we do nothing.
            }
        }

        /// <summary>
        /// Checks that the InteractionProvider has not been canceled.
        /// </summary>
        public void EnsureNotCanceled()
        {
            if (canceled)
                throw new SimulatorCanceledException();
        }

        public async Task WaitAsync(int millisecondsTimeout)
        {
            EnsureNotCanceled();

            /*
            Instead of using a wait method for the complete timeout (which is a bit inaccurate 
            as it may be up to ~ 15 ms longer than specified), we use the specified timeout - 12 to wait
            and then call Thread.SpinWait() to loop until the complete wait interval has been reached
            which we measure using a high-resolution timer.
            This means shortly before this method returns there will be a bit CPU usage but the actual
            time which we waited will be more accurate.
            */
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int waitTime = millisecondsTimeout - 15;
            if (waitTime > 0)
                await waitSemaphore.WaitAsync(waitTime);

            // For the remaining time, loop until the complete time has passed.
            while (true)
            {
                EnsureNotCanceled();

                long remaining = millisecondsTimeout - sw.ElapsedMilliseconds;
                if (remaining <= 0)
                    break;
                // 1000 iterations should take about 4 µs on a 3.4 GHz system.
                Thread.SpinWait(1000);
            }
        }

        private WindowPosition GetMainWindowPosition()
        {
            IntPtr hWnd = environmentInterface.FindMainWindowHandleOfProcess(process);
            return environmentInterface.GetWindowPosition(hWnd);
        }

        public WindowPosition GetCurrentWindowPosition()
        {
            EnsureNotCanceled();

            return GetMainWindowPosition();
        }

        public IScreenshotContent CreateCurrentWindowScreenshot()
        {
            EnsureNotCanceled();

            IntPtr hWnd = environmentInterface.FindMainWindowHandleOfProcess(process);
            return environmentInterface.CreateWindowScreenshot(hWnd);
        }

        public void PressKey(AbstractWindowsEnvironment.VirtualKeyShort key)
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            if (!keysCurrentlyPressed.Contains(key))
            {
                environmentInterface.PressKey(key);
                keysCurrentlyPressed.Add(key);
            }
        }

        public void ReleaseKey(AbstractWindowsEnvironment.VirtualKeyShort key)
        {
            EnsureNotCanceled();

            int kcpIdx = keysCurrentlyPressed.IndexOf(key);
            if (kcpIdx >= 0)
            {
                environmentInterface.ReleaseKey(key);
                keysCurrentlyPressed.RemoveAt(kcpIdx);
            }
        }

        public void WriteText(string text)
        {
            EnsureNotCanceled();
            environmentInterface.WriteText(text);
        }

        public void MoveMouse(Coordinates c)
        {
            MoveMouse(c.X, c.Y);
        }

        public void MoveMouse(int x, int y)
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            environmentInterface.MoveMouse(x, y);
        }

        public void PressMouseButton()
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            if (!isMouseButtonPressed)
            {
                environmentInterface.PressMouseButton();
                isMouseButtonPressed = true;
            }
        }


        public void ReleaseMouseButton()
        {
            EnsureNotCanceled();

            if (isMouseButtonPressed)
            {
                environmentInterface.ReleaseMouseButton();
                isMouseButtonPressed = false;
            }
        }

        ~StandardInteractionProvider()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of this StandardInteractionProvider.
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!canceled)
                    HandleCancelCallback();

                if (!disposed)
                {
                    disposed = true;

                    waitSemaphore.Dispose();

                    // Process can be null if the InteractionProvider was not initialized.
                    if (process != null)
                        process.Dispose();


                    // Release mouse buttons and keys that are currently pressed.
                    // Note that if another task is currently waiting in the WaitAsync() method, it can
                    // happen that it is continued after this method returns, but the WaitAsync() will
                    // throw an ActionCanceledException which the action shouldn't catch.
                    if (isMouseButtonPressed)
                    {
                        environmentInterface.ReleaseMouseButton();
                    }

                    foreach (AbstractWindowsEnvironment.VirtualKeyShort key in keysCurrentlyPressed)
                    {
                        environmentInterface.ReleaseKey(key);
                    }
                }
            }
        }
    }
}
