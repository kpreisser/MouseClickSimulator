using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly Simulator simulator;
        private readonly AbstractWindowsEnvironment environmentInterface;

        private Process process;
        private AbstractWindowsEnvironment.ScreenshotContent currentScreenshot;
        private bool isMouseButtonPressed = false;
        private List<AbstractWindowsEnvironment.VirtualKeyShort> keysCurrentlyPressed 
            = new List<AbstractWindowsEnvironment.VirtualKeyShort>();


        public StandardInteractionProvider(Simulator simulator, AbstractWindowsEnvironment environmentInterface,
            out Action cancelCallback)
        {
            this.simulator = simulator;
            this.environmentInterface = environmentInterface;
            cancelCallback = HandleCancelCallback;
        }


        public async Task InitializeAsync()
        {
            for (;;)
            {
                try
                {
                    process = environmentInterface.FindProcess();

                    // Bring the destination window to foreground.
                    IntPtr hWnd = environmentInterface.FindMainWindowHandleOfProcess(process);
                    environmentInterface.BringWindowToForeground(hWnd);

                    // Wait a bit so that the window can go into foreground.
                    await WaitSemaphoreInternalAsync(500, false);
                }
                catch (Exception ex) when (!(ex is SimulatorCanceledException))
                {
                    await CheckRetryForExceptionAsync(ex, false);
                    continue;
                }
                break;
            }
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

        public async Task CheckRetryForExceptionAsync(Exception ex) => await CheckRetryForExceptionAsync(ex, true);

        private async Task CheckRetryForExceptionAsync(Exception ex, bool reinitialize)
        {
            if (simulator.AsyncRetryHandler == null)
            {
                // Simply rethrow the exception.
                throw ex;
            }
            else
            {
                // Need to release active keys etc.
                CancelActiveInteractions();

                bool result = await simulator.AsyncRetryHandler(ex);
                if (!result)
                    throw new SimulatorCanceledException();

                // When trying again, we need to re-initialize.
                if (reinitialize)
                    await InitializeAsync();
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

        private async Task WaitSemaphoreInternalAsync(int milliseconds, bool checkWindowForeground = true)
        {
            EnsureNotCanceled();

            if (!checkWindowForeground)
            {
                if (await waitSemaphore.WaitAsync(Math.Max(0, milliseconds)))
                    EnsureNotCanceled();
            }
            else
            {
                // Wait max. 100 ms, and check if the TT window is still active.
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (true)
                {
                    // Check if the window is still active and in foreground.
                    GetMainWindowPosition();

                    long remaining = milliseconds - sw.ElapsedMilliseconds;
                    if (remaining <= 0)
                        break;

                    if (await waitSemaphore.WaitAsync(Math.Min((int)remaining, 100)))
                        EnsureNotCanceled();
                }
            }
        }

        public async Task WaitAsync(int millisecondsTimeout, bool useAccurateTimer = false)
        {
            if (useAccurateTimer)
            {
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
                await WaitSemaphoreInternalAsync(waitTime);

                // For the remaining time, loop until the complete time has passed.
                while (true)
                {
                    EnsureNotCanceled();

                    long remaining = millisecondsTimeout - sw.ElapsedMilliseconds;
                    if (remaining <= 0)
                        break;
                    // 1000 iterations should take about 40 µs on a 3.4 GHz system.
                    Thread.SpinWait(1000);
                }
            }
            else
            {
                await WaitSemaphoreInternalAsync(millisecondsTimeout);
            }
        }

        private WindowPosition GetMainWindowPosition() =>
            environmentInterface.GetWindowPosition(environmentInterface.FindMainWindowHandleOfProcess(process));
        

        public WindowPosition GetCurrentWindowPosition()
        {
            EnsureNotCanceled();

            return GetMainWindowPosition();
        }

        public IScreenshotContent GetCurrentWindowScreenshot()
        {
            EnsureNotCanceled();

            IntPtr hWnd = environmentInterface.FindMainWindowHandleOfProcess(process);
            currentScreenshot = environmentInterface.CreateWindowScreenshot(hWnd, currentScreenshot);
            return currentScreenshot;
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

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            environmentInterface.WriteText(text);
        }

        public void MoveMouse(Coordinates c) => MoveMouse(c.X, c.Y);
        
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

        private void CancelActiveInteractions()
        {
            // Release mouse buttons and keys that are currently pressed.
            if (isMouseButtonPressed)
            {
                environmentInterface.ReleaseMouseButton();
                isMouseButtonPressed = false;
            }

            foreach (AbstractWindowsEnvironment.VirtualKeyShort key in keysCurrentlyPressed)
            {
                environmentInterface.ReleaseKey(key);
            }
            keysCurrentlyPressed.Clear();
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

                    if (currentScreenshot != null)
                        currentScreenshot.Dispose();


                    CancelActiveInteractions();
                }
            }
        }

    }
}
