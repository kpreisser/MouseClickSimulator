using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Environment
{
    internal class StandardInteractionProvider : IInteractionProvider, IDisposable
    {
        private readonly object syncRoot = new object();

        private bool disposed = false;

        /// <summary>
        /// Specifies if this InteractionProvider has been canceled. This flag can be set by
        /// another thread while the simulator is running, therefore we need to lock on 'syncRoot' to
        /// access it.
        /// </summary>
        private bool canceled = false;

        private readonly SemaphoreSlim waitSemaphore = new SemaphoreSlim(0);

        private readonly Simulator simulator;
        private readonly AbstractWindowsEnvironment environmentInterface;
        private IntPtr windowHandle;

        private AbstractWindowsEnvironment.ScreenshotContent currentScreenshot;
        private bool isMouseButtonPressed = false;
        private List<AbstractWindowsEnvironment.VirtualKeyShort> keysCurrentlyPressed =
                new List<AbstractWindowsEnvironment.VirtualKeyShort>();

        private Coordinates lastMouseCoordinates = new Coordinates(0, 0);


        public StandardInteractionProvider(
                Simulator simulator,
                AbstractWindowsEnvironment environmentInterface,
                out Action cancelCallback)
        {
            this.simulator = simulator;
            this.environmentInterface = environmentInterface;
            cancelCallback = HandleCancelCallback;
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

        public async Task InitializeAsync()
        {
            while (true)
            {
                try
                {
                    var previousWindowToBringToForeground = IntPtr.Zero;
                    bool? lastInitializingEventParameter = null;
                    while (true)
                    {
                        // First, find the game processes. This will always return at least one process,
                        // or throw.
                        var processes = this.environmentInterface.FindProcesses();
                        if (processes.Count == 1)
                        {
                            if (lastInitializingEventParameter != false)
                            {
                                lastInitializingEventParameter = false;
                                this.simulator.OnSimulatorInitializing(lastInitializingEventParameter);
                            }

                            // When there is only one process, we simply bring the window to the
                            // foreground (if we didn't do it already).
                            this.windowHandle = this.environmentInterface.FindMainWindowHandleOfProcess(processes[0]);
                            if (this.windowHandle != previousWindowToBringToForeground)
                            {
                                previousWindowToBringToForeground = this.windowHandle;
                                this.environmentInterface.BringWindowToForeground(this.windowHandle);
                            }

                            // Wait a bit so that the window can go into foreground.
                            await WaitSemaphoreInternalAsync(250, false);

                            // If the window isn't in foreground, try again.
                            bool isInForeground;
                            this.environmentInterface.GetWindowPosition(this.windowHandle, out isInForeground, false);
                            if (isInForeground)
                                break;
                        }
                        else
                        {
                            if (lastInitializingEventParameter != true)
                            {
                                lastInitializingEventParameter = true;
                                this.simulator.OnSimulatorInitializing(lastInitializingEventParameter);
                            }

                            // When there are multiple processes, wait until on of the windows goes into foreground.
                            bool foundWindow = false;

                            foreach (var process in processes)
                            {
                                try
                                {
                                    var hWnd = this.environmentInterface.FindMainWindowHandleOfProcess(process);
                                    bool isInForeground;
                                    this.environmentInterface.GetWindowPosition(hWnd, out isInForeground, false);

                                    if (isInForeground)
                                    {
                                        // OK, we found our window to use.
                                        this.windowHandle = hWnd;
                                        foundWindow = true;
                                        break;
                                    }
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }

                            if (foundWindow)
                                break;

                            // If non of the windows is in foreground, wait a bit and try again.
                            await WaitSemaphoreInternalAsync(250, false);
                        }
                    }

                    this.simulator.OnSimulatorInitializing(null);
                }
                catch (Exception ex)
                {
                    this.simulator.OnSimulatorInitializing(null);

                    if (!(ex is SimulatorCanceledException))
                    {
                        await CheckRetryForExceptionAsync(ex, false);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
                break;
            }
        }

        /// <summary>
        /// Handles a callback to cancel the <see cref="StandardInteractionProvider"/>.
        /// This method can be called concurrently from a different thread while the simulator is running.
        /// </summary>
        private void HandleCancelCallback()
        {
            // Need to lock to ensure the semaphore is not disposed while we call Release() on it
            // from another thread.
            lock (this.syncRoot)
            {
                if (!this.canceled)
                {
                    this.canceled = true;
                    // Release the semaphore (so that a task that is waiting can continue).
                    this.waitSemaphore.Release();
                }
            }
        }

        public async Task CheckRetryForExceptionAsync(Exception ex)
        {
            await CheckRetryForExceptionAsync(ex, true);
        }

        private async Task CheckRetryForExceptionAsync(Exception ex, bool reinitialize)
        {
            if (this.simulator.AsyncRetryHandler == null)
            {
                // Simply rethrow the exception.
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            else
            {
                // Need to release active keys etc.
                CancelActiveInteractions();

                bool result = await this.simulator.AsyncRetryHandler(ex);
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
            lock (this.syncRoot)
            {
                if (this.canceled)
                    throw new SimulatorCanceledException();
            }
        }

        private async Task WaitSemaphoreInternalAsync(int milliseconds, bool checkWindowForeground = true)
        {
            EnsureNotCanceled();

            if (!checkWindowForeground)
            {
                if (await this.waitSemaphore.WaitAsync(Math.Max(0, milliseconds)))
                    EnsureNotCanceled();
            }
            else
            {
                // Wait max. 100 ms, and check if the TT window is still active.
                var sw = new Stopwatch();
                sw.Start();
                while (true)
                {
                    // Check if the window is still active and in foreground.
                    GetMainWindowPosition();

                    long remaining = milliseconds - sw.ElapsedMilliseconds;
                    if (remaining <= 0)
                        break;

                    if (await this.waitSemaphore.WaitAsync(Math.Min((int)remaining, 100)))
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
                as it may be up to ~ 15 ms longer than specified), we use the specified timeout - 15 to wait
                and then call Thread.SpinWait() to loop until the complete wait interval has been reached
                which we measure using a high-resolution timer.
                This means shortly before this method returns there will be a bit CPU usage but the actual
                time which we waited will be more accurate.
                */
                var sw = new Stopwatch();
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

                    // 100 iterations should take about 4 µs on a 3.4 GHz system
                    Thread.SpinWait(100);
                }
            }
            else
            {
                await WaitSemaphoreInternalAsync(millisecondsTimeout);
            }
        }

        private WindowPosition GetMainWindowPosition()
        {
            bool isInForeground;
            return this.environmentInterface.GetWindowPosition(this.windowHandle, out isInForeground);
        }

        public WindowPosition GetCurrentWindowPosition()
        {
            EnsureNotCanceled();

            return GetMainWindowPosition();
        }

        public IScreenshotContent GetCurrentWindowScreenshot()
        {
            EnsureNotCanceled();

            this.currentScreenshot = this.environmentInterface.CreateWindowScreenshot(
                    this.windowHandle, this.currentScreenshot);
            return this.currentScreenshot;
        }

        public void PressKey(AbstractWindowsEnvironment.VirtualKeyShort key)
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            if (!this.keysCurrentlyPressed.Contains(key))
            {
                this.environmentInterface.PressKey(key);
                this.keysCurrentlyPressed.Add(key);
            }
        }

        public void ReleaseKey(AbstractWindowsEnvironment.VirtualKeyShort key)
        {
            EnsureNotCanceled();

            int kcpIdx = this.keysCurrentlyPressed.IndexOf(key);
            if (kcpIdx >= 0)
            {
                this.environmentInterface.ReleaseKey(key);
                this.keysCurrentlyPressed.RemoveAt(kcpIdx);
            }
        }

        public void WriteText(string text)
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            this.environmentInterface.WriteText(text);
        }

        public void MoveMouse(int x, int y)
        {
            MoveMouse(new Coordinates(x, y));
        }

        public void MoveMouse(Coordinates c)
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            var pos = GetMainWindowPosition();

            // If it is, set the last mouse coordinates.
            this.lastMouseCoordinates = c;

            // Convert the relative coordinates to absolute ones, then simulate the click.
            var absoluteCoords = pos.RelativeToAbsoluteCoordinates(c);
            this.environmentInterface.MoveMouse(absoluteCoords.X, absoluteCoords.Y);
        }

        public void PressMouseButton()
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            if (!this.isMouseButtonPressed)
            {
                this.environmentInterface.PressMouseButton();
                this.isMouseButtonPressed = true;
            }
        }

        public void ReleaseMouseButton()
        {
            EnsureNotCanceled();

            if (this.isMouseButtonPressed)
            {
                this.environmentInterface.ReleaseMouseButton();
                this.isMouseButtonPressed = false;
            }
        }

        private void CancelActiveInteractions()
        {
            // Release mouse buttons and keys that are currently pressed.
            if (this.isMouseButtonPressed)
            {
                this.environmentInterface.ReleaseMouseButton();
                this.isMouseButtonPressed = false;
            }

            foreach (var key in this.keysCurrentlyPressed)
            {
                this.environmentInterface.ReleaseKey(key);
            }
            this.keysCurrentlyPressed.Clear();
        }

        /// <summary>
        /// Disposes of this StandardInteractionProvider.
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                bool doDispose = false;
                lock (this.syncRoot)
                {
                    if (!this.disposed)
                    {
                        this.disposed = true;
                        doDispose = true;

                        // Ensure the provider is canceled.
                        if (!this.canceled)
                            HandleCancelCallback();

                        this.waitSemaphore.Dispose();
                    }
                }

                if (doDispose)
                {
                    this.currentScreenshot?.Dispose();

                    CancelActiveInteractions();
                }
            }
        }
    }
}
