using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace TTMouseClickSimulator.Core.Environment;

public abstract class InteractionProvider : IInteractionProvider, IDisposable
{
    protected readonly WindowsEnvironment environmentInterface;

    private readonly bool backgroundMode;

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly SemaphoreSlim waitSemaphore = new(0);

    private readonly Simulator simulator;

    private readonly List<WindowsEnvironment.VirtualKey> keysCurrentlyPressed = new();

    private bool disposed;
    private IntPtr windowHandle;

    private WindowsEnvironment.ScreenshotContent? currentScreenshot;
    private bool pausedSinceLastScreenshot;

    private bool isMouseButtonPressed;

    // Window-relative (when using backgroundMode) or absolute mouse coordinates
    private (int x, int y)? lastSetMouseCoordinates;

    private bool windowIsDisabled;
    private bool windowIsTopmost;

    private bool canRetryOnException = true;

    public InteractionProvider(
            Simulator simulator,
            WindowsEnvironment environmentInterface,
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

    public void Initialize()
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
                    var processes = this.FindProcesses();

                    try
                    {
                        if (processes.Count is 1)
                        {
                            if (lastInitializingEventParameter is not false)
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
                                this.WaitCore(100, false);

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
                            if (lastInitializingEventParameter is not true)
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

                            // If none of the windows is in foreground, wait a bit and try again.
                            this.WaitCore(200, false);
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
                    if (this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.CaptureScreenshot))
                    {
                        // Verify that we actually can create a screenshot directly from the
                        // window instead of from the screen.
                        this.CaptureCurrentWindowScreenshot(isInitialization: true);
                    }

                    if (this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.MouseInput))
                    {
                        // When we require mouse input, disable the window, so that it is harder to
                        // interrupt our actions by user input. Note that it seems even though the
                        // window can't be activated by clicking into it, moving the mouse over it
                        // while we send the LBUTTONDOWN message can still interfere with our
                        // actions (can move the mouse pointer to the current cursor's position,
                        // and can release the mouse button).
                        // Additionally, it's possible to activate the window by clicking on it's
                        // task bar button, and then keyboard input will be possible.
                        this.environmentInterface.SetWindowEnabled(
                            this.windowHandle,
                            enabled: false);

                        this.windowIsDisabled = true;

                        if (lastInitializingEventParameter.Value)
                        {
                            // Wait a bit after there were multiple windows, so that the user can
                            // deactivate the window before the first mouse click.
                            // TODO: Waybe we should wait until the window has been deactivated
                            // again in this case before continuing, so the mouse input isn't
                            // interrupted afterwards when the user then deactivates the window.
                            this.Wait(800);
                        }
                    }

                    // Wait a bit before starting.
                    this.Wait(200);
                }
                else
                {
                    if (this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.MouseInput) ||
                        this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.CaptureScreenshot))
                    {
                        // When we require mouse input or screenshots in non-background mode, set
                        // the window to topmost, to ensure other topmost windows don't hide the
                        // area that we want to click or scan.
                        this.windowIsTopmost = this.environmentInterface.TrySetWindowTopmost(
                            this.windowHandle,
                            topmost: true);
                    }

                    // Also wait a short time when not using background mode.
                    this.Wait(200);
                }
            }
            catch (Exception ex)
            {
                this.simulator.OnSimulatorInitializing(null);

                if (ex is not OperationCanceledException)
                {
                    this.CheckRetryForException(ex, false);
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

    public void CheckRetryForException(Exception ex)
    {
        this.CheckRetryForException(ex, reinitialize: true);
    }

    public void Wait(int millisecondsTimeout, bool useAccurateTimer = false)
    {
        if (useAccurateTimer)
        {
            // Instead of using a wait method for the complete timeout (which may be a
            // bit inaccurate depending on the OS timer resolution), we use the specified
            // timeout minus 5 to wait, and then call Thread.SpinWait() to loop until the
            // complete wait interval has been reached, which we measure using a
            // high-resolution timer.
            // This means shortly before this method returns there will be a bit CPU
            // usage but the actual time which we waited will be more accurate.
            // Note: On Windows, the timer resolution can be configured with the
            // timeBeginPeriod API, which we call on start-up.
            var sw = new Stopwatch();
            sw.Start();

            int waitTime = millisecondsTimeout - 5;
            this.WaitCore(waitTime);

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
            this.WaitCore(millisecondsTimeout);
        }
    }

    public WindowPosition GetCurrentWindowPosition()
    {
        this.CancellationToken.ThrowIfCancellationRequested();

        return this.GetWindowPositionCore();
    }

    public IScreenshotContent GetCurrentWindowScreenshot()
    {
        this.ThrowIfCapabilityNotSet(SimulatorCapabilities.CaptureScreenshot);
        this.CancellationToken.ThrowIfCancellationRequested();

        // Only capture a new screenshot if we paused since getting the last once, so that we
        // don't unnecessarily create new ones if nearly no time has passed since then.
        // For example, the AutomaticFishingAction might want to get a screenshot after
        // casting to check for an error dialog, but then immediately needs to get a screenshot
        // again to check for fish bubbles.
        if (this.currentScreenshot is null || this.pausedSinceLastScreenshot)
        {
            this.CaptureCurrentWindowScreenshot();
            this.pausedSinceLastScreenshot = false;
        }

        return this.currentScreenshot!;
    }

    public void PressKey(WindowsEnvironment.VirtualKey key)
    {
        this.ThrowIfCapabilityNotSet(SimulatorCapabilities.KeyboardInput);
        this.CancellationToken.ThrowIfCancellationRequested();

        // Check if the window is still active and in foreground.
        this.GetWindowPositionCore(failIfMinimized: !this.backgroundMode);

        // Allow the subclass to transform the key (e.g. change arrow keys to WASD
        // keys).
        this.TransformVirtualKey(ref key);

        if (!this.keysCurrentlyPressed.Contains(key))
        {
            if (!this.backgroundMode)
                this.environmentInterface.PressKey(key);
            else
                this.environmentInterface.PressWindowKey(this.windowHandle, key);

            this.keysCurrentlyPressed.Add(key);
        }
    }

    public void ReleaseKey(WindowsEnvironment.VirtualKey key)
    {
        this.ThrowIfCapabilityNotSet(SimulatorCapabilities.KeyboardInput);
        this.CancellationToken.ThrowIfCancellationRequested();

        // Allow the subclass to transform the key (e.g. change arrow keys to WASD
        // keys).
        this.TransformVirtualKey(ref key);

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
        this.ThrowIfCapabilityNotSet(SimulatorCapabilities.KeyboardInput);
        this.CancellationToken.ThrowIfCancellationRequested();

        // Check if the window is still active and in foreground.
        this.GetWindowPositionCore(failIfMinimized: !this.backgroundMode);

        if (!this.backgroundMode)
            this.environmentInterface.WriteText(text);
        else
            this.environmentInterface.WriteWindowText(this.windowHandle, text);
    }

    public void MoveMouse(Coordinates c)
    {
        // Note: We need to use Floor instead of Round for the mouse coordinates.
        // This is because they may result from scaling (where the actual
        // coordinates are always smaller than the width/height), and otherwise
        // we may get coordinates that equal the width/height, which means they
        // are outside of the window client area.
        // For example, consider x-coordinates for width 4 (0, 1, 2, 3) which are
        // scaled for width 2. The resulting doubles would be (0, 0.5, 1, 1.5).
        // If we round them, they would result in (0, 1, 1, 2), which is obviously
        // wrong as 2 is already the full width. Instead, we use Floor which results
        // in (0, 0, 1, 1).
        this.MoveMouse(
            checked((int)MathF.Floor(c.X)),
            checked((int)MathF.Floor(c.Y)));
    }

    public void MoveMouse(int x, int y)
    {
        this.ThrowIfCapabilityNotSet(SimulatorCapabilities.MouseInput);
        this.CancellationToken.ThrowIfCancellationRequested();

        if (!this.backgroundMode)
        {
            // Check if the window is still active and in foreground.
            var pos = this.GetWindowPositionCore();

            // Convert the relative coordinates to absolute ones, then simulate the click.
            var (absoluteX, absoluteY) = pos.RelativeToAbsoluteCoordinates(x, y);
            this.environmentInterface.MoveMouse(absoluteX, absoluteY);

            this.lastSetMouseCoordinates = (absoluteX, absoluteY);
        }
        else
        {
            this.environmentInterface.MoveWindowMouse(
                this.windowHandle,
                x,
                y,
                this.isMouseButtonPressed);

            this.lastSetMouseCoordinates = (x, y);
        }
    }

    public void PressMouseButton()
    {
        this.ThrowIfCapabilityNotSet(SimulatorCapabilities.MouseInput);
        this.CancellationToken.ThrowIfCancellationRequested();

        if (this.lastSetMouseCoordinates is null)
        {
            throw new InvalidOperationException(
                "Current mouse coordinates have not been set.");
        }

        if (!this.backgroundMode)
        {
            // Check if the window is still active and in foreground.
            this.GetWindowPositionCore();

            if (!this.isMouseButtonPressed)
            {
                this.environmentInterface.PressMouseButton(
                    this.lastSetMouseCoordinates.Value.x,
                    this.lastSetMouseCoordinates.Value.y);

                this.isMouseButtonPressed = true;
            }
        }
        else
        {
            if (!this.isMouseButtonPressed)
            {
                this.environmentInterface.PressWindowMouseButton(
                    this.windowHandle,
                    this.lastSetMouseCoordinates.Value.x,
                    this.lastSetMouseCoordinates.Value.y);

                this.isMouseButtonPressed = true;
            }
        }
    }

    public void ReleaseMouseButton()
    {
        this.ThrowIfCapabilityNotSet(SimulatorCapabilities.MouseInput);
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
                    this.lastSetMouseCoordinates.Value.x,
                    this.lastSetMouseCoordinates.Value.y);
            }

            this.isMouseButtonPressed = false;
        }
    }

    public void CancelActiveInteractions()
    {
        // Release mouse buttons and keys that are currently pressed, and revert changes to the
        // window. We need to ignore exceptions here as this method should never fail.
        if (this.isMouseButtonPressed)
        {
            try
            {
                if (!this.backgroundMode)
                {
                    this.environmentInterface.ReleaseMouseButton();
                }
                else
                {
                    this.environmentInterface.ReleaseWindowMouseButton(
                        this.windowHandle,
                        this.lastSetMouseCoordinates!.Value.x,
                        this.lastSetMouseCoordinates.Value.y);
                }
            }
            catch
            {
                // Ignore.
            }

            this.isMouseButtonPressed = false;
        }

        foreach (var key in this.keysCurrentlyPressed)
        {
            try
            {
                if (!this.backgroundMode)
                    this.environmentInterface.ReleaseKey(key);
                else
                    this.environmentInterface.ReleaseWindowKey(this.windowHandle, key);
            }
            catch
            {
                // Ignore.
            }
        }

        this.keysCurrentlyPressed.Clear();

        if (this.windowIsDisabled)
        {
            try
            {
                this.environmentInterface.SetWindowEnabled(this.windowHandle, enabled: true);
            }
            catch
            {
                // Ignore.
            }

            this.windowIsDisabled = false;
        }

        if (this.windowIsTopmost)
        {
            try
            {
                this.windowIsTopmost = this.environmentInterface.TrySetWindowTopmost(
                    this.windowHandle,
                    topmost: false);
            }
            catch
            {
                // Ignore.
            }

            this.windowIsTopmost = false;
        }
    }

    protected abstract List<Process> FindProcesses();

    protected virtual void TransformVirtualKey(ref WindowsEnvironment.VirtualKey key)
    {
        // Do nothing.
    }

    protected virtual void ValidateWindowPositionAndSize(WindowPosition windowPosition)
    {
        // Do nothing.
    }

    private void ThrowIfCapabilityNotSet(SimulatorCapabilities capabilities)
    {
        if (!this.simulator.RequiredCapabilities.IsSet(capabilities))
        {
            throw new InvalidOperationException(
                $"Capability '{capabilities}' has not been declared by one of the actions.");
        }
    }

    private void WaitCore(int milliseconds, bool checkWindow = true)
    {
        this.CancellationToken.ThrowIfCancellationRequested();
        this.pausedSinceLastScreenshot = true;

        if (!checkWindow)
        {
            this.waitSemaphore.Wait(
                Math.Max(0, milliseconds),
                this.CancellationToken);
        }
        else
        {
            // Wait at most 100 ms, and check if the window is still active.
            var sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                // Check if the window is still active (and, if not using background mode,
                // in foreground).
                this.GetWindowPositionCore(failIfMinimized: !this.backgroundMode);

                long remaining = milliseconds - sw.ElapsedMilliseconds;
                if (remaining <= 0)
                    break;

                this.waitSemaphore.Wait(
                    Math.Min((int)remaining, 100),
                    this.CancellationToken);
            }
        }
    }

    private WindowPosition GetWindowPositionCore(bool failIfMinimized = true)
    {
        // Fail if the window is no longer in foreground (active) and we are not using
        // background mode.
        var windowPosition = this.environmentInterface.GetWindowPosition(
            this.windowHandle,
            out _,
            failIfNotInForeground: !this.backgroundMode,
            failIfMinimized: failIfMinimized);

        // When we use mouse input or capture screenshots, allow the subclass to validate the
        // window location and size.
        if ((this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.MouseInput) ||
            this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.CaptureScreenshot)))
        {
            this.ValidateWindowPositionAndSize(windowPosition);

            // TODO: Check if the window is beyond the virtual screen size (if in non-background
            // mode and we require mouse or screenshot capabilities, or if in background mode
            // and we require screenshot capabilities).
        }

        return windowPosition;
    }

    private void CaptureCurrentWindowScreenshot(bool isInitialization = false)
    {
        this.CancellationToken.ThrowIfCancellationRequested();

        // When using background mode, create the screenshot directly from the window's
        // DC instead of from the whole screen. However, this seems not always to work
        // (apparently on older Windows versions), e.g. on a Windows 8.1 machine I just
        // got a black screen using this method. Also, e.g. with DirectX games on
        // Windows 10 and 11, this might not work.
        // Therefore, when not using background mode, we still create a screenshot from
        // the whole screen, which should work in every case (and the window also needs
        // to be in the foreground in this mode).
        // Currently, there doesn't seem another easy way to get a screenshot froom a
        // window client area if using the DC doesn't work (e.g. creating a DWM thumbnail
        // won't allow us to access the pixel data). If this will generally no longer work
        // with a future verison of the game, we may need to revert using the screen copy
        // also for background mode, but set the game window as topmost window.
        bool fromScreen = !this.backgroundMode;
        var windowPosition = this.GetWindowPositionCore();
        this.environmentInterface.CreateWindowScreenshot(
            this.windowHandle,
            windowPosition,
            ref this.currentScreenshot,
            fromScreen: fromScreen);

        if (!fromScreen && isInitialization)
        {
            // If we took the first screenshot from the window rather than the screen,
            // check whether it only contains black pixels. In that case, throw an
            // exception to inform the user that background mode won't work.
            if (this.currentScreenshot.ContainsOnlyBlackPixels())
            {
                // Don't allow to retry in this case since it would lead to the same
                // exception.
                this.canRetryOnException = false;
                throw new InvalidOperationException(
                    "Couldn't capture screenshot directly from window. " +
                    "Please disable background mode and try again.");
            }
        }
    }

    private void CheckRetryForException(Exception ex, bool reinitialize)
    {
        if (!this.canRetryOnException || this.simulator.RetryHandler is null)
        {
            // Simply rethrow the exception.
            ExceptionDispatchInfo.Throw(ex);
        }
        else
        {
            // Need to release active keys etc.
            this.CancelActiveInteractions();

            bool result = this.simulator.RetryHandler(ex);
            if (!result)
            {
                this.cancellationTokenSource.Cancel();
                this.CancellationToken.ThrowIfCancellationRequested();
            }

            // When trying again, we need to re-initialize.
            if (reinitialize)
                this.Initialize();
        }
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
                this.waitSemaphore.Dispose();
            }
        }
    }
}
