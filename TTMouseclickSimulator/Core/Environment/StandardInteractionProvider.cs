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

        private Process process;
        private AbstractWindowsEnvironment.ScreenshotContent currentScreenshot;
        private bool isMouseButtonPressed = false;
        private List<AbstractWindowsEnvironment.VirtualKeyShort> keysCurrentlyPressed =
                new List<AbstractWindowsEnvironment.VirtualKeyShort>();


        public StandardInteractionProvider(Simulator simulator, AbstractWindowsEnvironment environmentInterface,
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
            for (;;)
            {
                try
                {
                    this.process = this.environmentInterface.FindProcess();

                    // Bring the destination window to foreground.
                    var hWnd = this.environmentInterface.FindMainWindowHandleOfProcess(this.process);
                    this.environmentInterface.BringWindowToForeground(hWnd);

                    // Wait a bit so that the window can go into foreground.
                    await WaitSemaphoreInternalAsync(500, false);
                }
                catch (Exception ex) when (!(ex is SimulatorCanceledException))
                {
                    await CheckRetryForExceptionAsync(ExceptionDispatchInfo.Capture(ex), false);
                    continue;
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

        public async Task CheckRetryForExceptionAsync(ExceptionDispatchInfo ex)
        {
            await CheckRetryForExceptionAsync(ex, true);
        }

        private async Task CheckRetryForExceptionAsync(ExceptionDispatchInfo ex, bool reinitialize)
        {
            if (this.simulator.AsyncRetryHandler == null)
            {
                // Simply rethrow the exception.
                ex.Throw();
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
            return this.environmentInterface.GetWindowPosition(this.environmentInterface.FindMainWindowHandleOfProcess(this.process));
        }
        

        public WindowPosition GetCurrentWindowPosition()
        {
            EnsureNotCanceled();

            return GetMainWindowPosition();
        }

        public IScreenshotContent GetCurrentWindowScreenshot()
        {
            EnsureNotCanceled();

            var hWnd = this.environmentInterface.FindMainWindowHandleOfProcess(this.process);
            this.currentScreenshot = this.environmentInterface.CreateWindowScreenshot(hWnd, this.currentScreenshot);
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

        public void MoveMouse(Coordinates c)
        {
            MoveMouse(c.X, c.Y);
        }
        
        public void MoveMouse(int x, int y)
        {
            EnsureNotCanceled();

            // Check if the window is still active and in foreground.
            GetMainWindowPosition();

            this.environmentInterface.MoveMouse(x, y);
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
                    // Process can be null if the InteractionProvider was not initialized.
                    this.process?.Dispose();
                    this.currentScreenshot?.Dispose();

                    CancelActiveInteractions();
                }
            }
        }
    }
}
