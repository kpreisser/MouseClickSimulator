using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Environment;

internal class StandardInteractionProvider : IInteractionProvider, IDisposable
{
    private readonly bool backgroundMode;

    private readonly CancellationTokenSource cancellationTokenSource = new();

    private readonly Simulator simulator;
    private readonly AbstractWindowsEnvironment environmentInterface;

    private readonly List<AbstractWindowsEnvironment.VirtualKey> keysCurrentlyPressed = new();

    private bool disposed;
    private IntPtr windowHandle;

    private AbstractWindowsEnvironment.ScreenshotContent? currentScreenshot;

    private bool isMouseButtonPressed;

    // Window-relative (when using backgroundMode) or absolute mouse coordinates
    private Coordinates? lastSetMouseCoordinates;

    public StandardInteractionProvider(
            Simulator simulator,
            AbstractWindowsEnvironment environmentInterface,
            bool backgroundMode)
    {
        this.simulator = simulator;
        this.environmentInterface = environmentInterface;
        this.backgroundMode = backgroundMode;
    }

    public CancellationToken CancellationToken
    {
        get => this.cancellationTokenSource.Token;
    }

    /// <summary>
    /// Cancels this provider. This method can be called from different thread(s)
    /// while the simulator is running.
    /// </summary>
    public void Cancel()
    {
        this.cancellationTokenSource.Cancel();
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
                            this.windowHandle = this.environmentInterface.FindMainWindowHandleOfProcess(
                                processes[0]);

                            if (!this.backgroundMode)
                            {
                                if (this.windowHandle != previousWindowToBringToForeground)
                                {
                                    previousWindowToBringToForeground = this.windowHandle;
                                    this.environmentInterface.BringWindowToForeground(this.windowHandle);
                                }

                                // Wait a bit so that the window can go into foreground.
                                await this.WaitCoreAsync(250, false);

                                // If the window isn't in foreground, try again.
                                this.environmentInterface.GetWindowPosition(
                                    this.windowHandle,
                                    out bool isInForeground,
                                    false);

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

                                    this.environmentInterface.GetWindowPosition(
                                        hWnd,
                                        out bool isInForeground,
                                        false);

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
                            await this.WaitCoreAsync(250, false);
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
            catch (Exception ex)
            {
                this.simulator.OnSimulatorInitializing(null);

                if (ex is not OperationCanceledException)
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
    }

    public ValueTask CheckRetryForExceptionAsync(Exception ex)
    {
        return this.CheckRetryForExceptionAsync(ex, true);
    }

    public async ValueTask WaitAsync(int millisecondsTimeout, bool useAccurateTimer = false)
    {
        if (useAccurateTimer)
        {
            // Instead of using a wait method for the complete timeout (which is a bit
            // inaccurate depending on the OS timer resolution, we use the specified
            // timeout - 15 to wait and then call Thread.SpinWait() to loop until the
            // complete wait interval has been reached which we measure using a
            // high-resolution timer.
            // This means shortly before this method returns there will be a bit CPU
            // usage but the actual time which we waited will be more accurate.
            var sw = new Stopwatch();
            sw.Start();

            int waitTime = millisecondsTimeout - 15;
            await this.WaitCoreAsync(waitTime);

            // For the remaining time, loop until the complete time has passed.
            while (true)
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                long remaining = millisecondsTimeout - sw.ElapsedMilliseconds;
                if (remaining <= 0)
                    break;

                // 100 iterations should take about 4 µs on a 3.4 GHz system
                Thread.SpinWait(100);
            }
        }
        else
        {
            await this.WaitCoreAsync(millisecondsTimeout);
        }
    }

    public WindowPosition GetCurrentWindowPosition()
    {
        this.CancellationToken.ThrowIfCancellationRequested();

        return this.GetMainWindowPosition();
    }

    public IScreenshotContent GetCurrentWindowScreenshot()
    {
        this.CancellationToken.ThrowIfCancellationRequested();

        this.environmentInterface.CreateWindowScreenshot(
            this.windowHandle,
            ref this.currentScreenshot,
            !this.backgroundMode);

        return this.currentScreenshot;
    }

    public void PressKey(AbstractWindowsEnvironment.VirtualKey key)
    {
        this.CancellationToken.ThrowIfCancellationRequested();

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
        this.CancellationToken.ThrowIfCancellationRequested();

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
        this.CancellationToken.ThrowIfCancellationRequested();

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
        this.CancellationToken.ThrowIfCancellationRequested();

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
        this.CancellationToken.ThrowIfCancellationRequested();

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
                if (this.lastSetMouseCoordinates is null)
                {
                    throw new InvalidOperationException(
                        "Current mouse coordinates have not been set.");
                }

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
        this.CancellationToken.ThrowIfCancellationRequested();

        if (this.isMouseButtonPressed)
        {
            if (!this.backgroundMode)
            {
                this.environmentInterface.ReleaseMouseButton();
            }
            else
            {
                if (this.lastSetMouseCoordinates is null)
                {
                    throw new InvalidOperationException(
                        "Current mouse coordinates have not been set.");
                }

                this.environmentInterface.ReleaseWindowMouseButton(
                   this.windowHandle,
                   this.lastSetMouseCoordinates.Value.X,
                   this.lastSetMouseCoordinates.Value.Y);
            }

            this.isMouseButtonPressed = false;
        }
    }

    public void CancelActiveInteractions()
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
                   this.lastSetMouseCoordinates!.Value.X,
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

    private async ValueTask WaitCoreAsync(int milliseconds, bool checkWindow = true)
    {
        this.CancellationToken.ThrowIfCancellationRequested();

        if (!checkWindow)
        {
            await Task.Delay(
                Math.Max(0, milliseconds),
                this.CancellationToken);
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

                await Task.Delay(
                    Math.Min((int)remaining, 100),
                    this.CancellationToken);
            }
        }
    }

    private async ValueTask CheckRetryForExceptionAsync(Exception ex, bool reinitialize)
    {
        if (this.simulator.AsyncRetryHandler is null)
        {
            // Simply rethrow the exception.
            ExceptionDispatchInfo.Throw(ex);
        }
        else
        {
            // Need to release active keys etc.
            this.CancelActiveInteractions();

            bool result = await this.simulator.AsyncRetryHandler(ex);
            if (!result)
            {
                this.cancellationTokenSource.Cancel();
                this.CancellationToken.ThrowIfCancellationRequested();
            }

            // When trying again, we need to re-initialize.
            if (reinitialize)
                await this.InitializeAsync();
        }
    }

    private WindowPosition GetMainWindowPosition(bool failIfMinimized = true)
    {
        // Fail if the window is no longer in foreground (active) and we are not
        // using background mode.
        return this.environmentInterface.GetWindowPosition(
            this.windowHandle,
            out _,
            !this.backgroundMode,
            failIfMinimized);
    }

    /// <summary>
    /// Disposes of this StandardInteractionProvider.
    /// </summary>
    /// <param name="disposing"></param>
    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                this.CancelActiveInteractions();

                this.currentScreenshot?.Dispose();
                this.cancellationTokenSource.Dispose();
            }
        }
    }
}
