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

        private readonly bool backgroundMode;

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
        private List<AbstractWindowsEnvironment.VirtualKey> keysCurrentlyPressed =
                new List<AbstractWindowsEnvironment.VirtualKey>();

        // Window-relative (when using backgroundMode) or absolute mouse coordinates
        private Coordinates? lastSetMouseCoordinates;

        public StandardInteractionProvider(
                Simulator simulator,
                AbstractWindowsEnvironment environmentInterface,
                bool backgroundMode,
                out Action cancelCallback)
        {
            this.simulator = simulator;
            this.environmentInterface = environmentInterface;
            this.backgroundMode = backgroundMode;
            cancelCallback = this.HandleCancelCallback;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task InitializeAsync()
        {
            this.lastSetMouseCoordinates = null;

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
                        try
                        {
                            if (processes.Count == 1)
                            {
                                if (lastInitializingEventParameter != false)
                                {
                                    lastInitializingEventParameter = false;
                                    this.simulator.OnSimulatorInitializing(lastInitializingEventParameter);
                                }

                                // When there is only one process, we simply bring the window to
                                // the foreground (if we didn't do it already).
                                this.windowHandle = this.environmentInterface.FindMainWindowHandleOfProcess(processes[0]);

                                if (!this.backgroundMode)
                                {
                                    if (this.windowHandle != previousWindowToBringToForeground)
                                    {
                                        previousWindowToBringToForeground = this.windowHandle;
                                        this.environmentInterface.BringWindowToForeground(this.windowHandle);
                                    }

                                    // Wait a bit so that the window can go into foreground.
                                    await this.WaitSemaphoreInternalAsync(250, false);

                                    // If the window isn't in foreground, try again.
                                    bool isInForeground;
                                    this.environmentInterface.GetWindowPosition(this.windowHandle, out isInForeground, false);
                                    if (isInForeground)
                                        break;
                                }
                                else
                                {
                                    // In background mode, we don't need to bring the window
                                    // into foreground.
                                    break;
                                }
                            }
                            else
                            {
                                if (lastInitializingEventParameter != true)
                                {
                                    lastInitializingEventParameter = true;
                                    this.simulator.OnSimulatorInitializing(lastInitializingEventParameter);
                                }

                                // When there are multiple processes, wait until on of the windows
                                // goes into foreground.
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
                                await this.WaitSemaphoreInternalAsync(250, false);
                            }
                        }
                        finally
                        {
                            // Dispose of the processes after using them.
                            foreach (var process in processes)
                                process.Dispose();
                        }
                    }

                    this.simulator.OnSimulatorInitializing(null);
                }
                catch (Exception ex)
                {
                    this.simulator.OnSimulatorInitializing(null);

                    if (!(ex is SimulatorCanceledException))
                    {
                        await this.CheckRetryForExceptionAsync(ex, false);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }

                break;
            }

            if (this.backgroundMode)
            {
                // When starting in background mode, wait a second before doing anything, to
                // handle the case when the user clicks into the window to activate it
                // (when more than one processes were detected).
                await this.WaitAsync(900);

                // Then, simulate a click to (0, 0). This seems currently to be necessary so
                // that the first WM_LBUTTONDOWN that we send to the window works correctly
                // if the window is currently inactive (otherwise, the first message would
                // have the effect that the mouse button is pressed but is then immediately
                // released; probably due to a WM_MOUSELEAVE message being sent by Windows).
                this.MoveMouse(0, 0);
                this.PressMouseButton();
                await this.WaitAsync(50);
                this.ReleaseMouseButton();
                await this.WaitAsync(50);
            }
            else
            {
                // Also wait a short time when not using background mode.
                await this.WaitAsync(500);
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

        public Task CheckRetryForExceptionAsync(Exception ex)
        {
            return this.CheckRetryForExceptionAsync(ex, true);
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
                this.CancelActiveInteractions();

                bool result = await this.simulator.AsyncRetryHandler(ex);
                if (!result)
                    throw new SimulatorCanceledException();

                // When trying again, we need to re-initialize.
                if (reinitialize)
                    await this.InitializeAsync();
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

        private async Task WaitSemaphoreInternalAsync(int milliseconds, bool checkWindow = true)
        {
            this.EnsureNotCanceled();

            if (!checkWindow)
            {
                if (await this.waitSemaphore.WaitAsync(Math.Max(0, milliseconds)))
                    this.EnsureNotCanceled();
            }
            else
            {
                // Wait max. 100 ms, and check if the TT window is still active.
                var sw = new Stopwatch();
                sw.Start();
                while (true)
                {
                    // Check if the window is still active (and, if not using background mode,
                    // in foreground).
                    this.GetMainWindowPosition(failIfMinimized: !this.backgroundMode);

                    long remaining = milliseconds - sw.ElapsedMilliseconds;
                    if (remaining <= 0)
                        break;

                    if (await this.waitSemaphore.WaitAsync(Math.Min((int)remaining, 100)))
                        this.EnsureNotCanceled();
                }
            }
        }

        public async Task WaitAsync(int millisecondsTimeout, bool useAccurateTimer = false)
        {
            if (useAccurateTimer)
            {
                /*
                 * Instead of using a wait method for the complete timeout (which is a bit
                 * inaccurate depending on the OS timer resolution, we use the specified
                 * timeout - 5 to wait and then call Thread.SpinWait() to loop until the
                 * complete wait interval has been reached which we measure using a
                 * high-resolution timer.
                 * This means shortly before this method returns there will be a bit CPU
                 * usage but the actual time which we waited will be more accurate.
                 */
                var sw = new Stopwatch();
                sw.Start();

                int waitTime = millisecondsTimeout - 5;
                await this.WaitSemaphoreInternalAsync(waitTime);

                // For the remaining time, loop until the complete time has passed.
                while (true)
                {
                    this.EnsureNotCanceled();

                    long remaining = millisecondsTimeout - sw.ElapsedMilliseconds;
                    if (remaining <= 0)
                        break;

                    // 100 iterations should take about 4 µs on a 3.4 GHz system
                    Thread.SpinWait(100);
                }
            }
            else
            {
                await this.WaitSemaphoreInternalAsync(millisecondsTimeout);
            }
        }

        private WindowPosition GetMainWindowPosition(bool failIfMinimized = true)
        {
            // Fail if the window is no longer in foreground (active) and we are not
            // using background mode.
            bool isInForeground;
            return this.environmentInterface.GetWindowPosition(
                this.windowHandle,
                out isInForeground,
                !this.backgroundMode,
                failIfMinimized);
        }

        public WindowPosition GetCurrentWindowPosition()
        {
            this.EnsureNotCanceled();

            return this.GetMainWindowPosition();
        }

        public IScreenshotContent GetCurrentWindowScreenshot()
        {
            this.EnsureNotCanceled();

            this.environmentInterface.CreateWindowScreenshot(
                this.windowHandle,
                ref this.currentScreenshot,
                !this.backgroundMode);

            return this.currentScreenshot;
        }

        public void PressKey(AbstractWindowsEnvironment.VirtualKey key)
        {
            this.EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            this.GetMainWindowPosition(failIfMinimized: !this.backgroundMode);

            if (!this.keysCurrentlyPressed.Contains(key))
            {
                if (!this.backgroundMode)
                    this.environmentInterface.PressKey(key);
                else
                    this.environmentInterface.PressWindowKey(this.windowHandle, key);

                this.keysCurrentlyPressed.Add(key);
            }
        }

        public void ReleaseKey(AbstractWindowsEnvironment.VirtualKey key)
        {
            this.EnsureNotCanceled();

            int kcpIdx = this.keysCurrentlyPressed.IndexOf(key);
            if (kcpIdx >= 0)
            {
                if (!this.backgroundMode)
                    this.environmentInterface.ReleaseKey(key);
                else
                    this.environmentInterface.ReleaseWindowKey(this.windowHandle, key);

                this.keysCurrentlyPressed.RemoveAt(kcpIdx);
            }
        }

        public void WriteText(string text)
        {
            this.EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            this.GetMainWindowPosition(failIfMinimized: !this.backgroundMode);

            if (!this.backgroundMode)
                this.environmentInterface.WriteText(text);
            else
                this.environmentInterface.WriteWindowText(this.windowHandle, text);
        }

        public void MoveMouse(int x, int y)
        {
            this.MoveMouse(new Coordinates(x, y));
        }

        public void MoveMouse(Coordinates c)
        {
            this.EnsureNotCanceled();

            if (!this.backgroundMode)
            {
                // Check if the window is still active and in foreground.
                var pos = this.GetMainWindowPosition();

                // Convert the relative coordinates to absolute ones, then simulate the click.
                var absoluteCoords = pos.RelativeToAbsoluteCoordinates(c);
                this.environmentInterface.MoveMouse(absoluteCoords.X, absoluteCoords.Y);
                this.lastSetMouseCoordinates = absoluteCoords;
            }
            else
            {
                this.environmentInterface.MoveWindowMouse(
                    this.windowHandle,
                    c.X,
                    c.Y,
                    this.isMouseButtonPressed);

                this.lastSetMouseCoordinates = c;
            }
        }

        public void PressMouseButton()
        {
            this.EnsureNotCanceled();

            if (!this.backgroundMode)
            {
                // Check if the window is still active and in foreground.
                this.GetMainWindowPosition();

                if (!this.isMouseButtonPressed)
                {
                    this.environmentInterface.PressMouseButton();
                    this.isMouseButtonPressed = true;
                }
            }
            else
            {
                if (!this.isMouseButtonPressed)
                {
                    this.environmentInterface.PressWindowMouseButton(
                        this.windowHandle,
                        this.lastSetMouseCoordinates.Value.X,
                        this.lastSetMouseCoordinates.Value.Y);

                    this.isMouseButtonPressed = true;
                }
            }
        }

        public void ReleaseMouseButton()
        {
            this.EnsureNotCanceled();

            if (this.isMouseButtonPressed)
            {
                if (!this.backgroundMode)
                {
                    this.environmentInterface.ReleaseMouseButton();
                }
                else
                {
                    this.environmentInterface.ReleaseWindowMouseButton(
                       this.windowHandle,
                       this.lastSetMouseCoordinates.Value.X,
                       this.lastSetMouseCoordinates.Value.Y);
                }

                this.isMouseButtonPressed = false;
            }
        }

        private void CancelActiveInteractions()
        {
            // Release mouse buttons and keys that are currently pressed.
            if (this.isMouseButtonPressed)
            {
                if (!this.backgroundMode)
                {
                    this.environmentInterface.ReleaseMouseButton();
                }
                else
                {
                    this.environmentInterface.ReleaseWindowMouseButton(
                       this.windowHandle,
                       this.lastSetMouseCoordinates.Value.X,
                       this.lastSetMouseCoordinates.Value.Y);
                }

                this.isMouseButtonPressed = false;
            }

            foreach (var key in this.keysCurrentlyPressed)
            {
                if (!this.backgroundMode)
                    this.environmentInterface.ReleaseKey(key);
                else
                    this.environmentInterface.ReleaseWindowKey(this.windowHandle, key);
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
                            this.HandleCancelCallback();

                        this.waitSemaphore.Dispose();
                    }
                }

                if (doDispose)
                {
                    this.currentScreenshot?.Dispose();

                    this.CancelActiveInteractions();
                }
            }
        }
    }
}
