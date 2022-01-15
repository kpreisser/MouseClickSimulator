﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Environment;

internal class InteractionProvider : IInteractionProvider, IDisposable
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

    private bool windowIsDisabled;
    private bool windowIsTopmost;

    private bool canRetryOnException = true;

    public InteractionProvider(
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
                    if (this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.CaptureScreenshot))
                    {
                        // Verify that we actually can create a screenshot directly from the
                        // window instead of from the screen.
                        this.GetCurrentWindowScreenshot(isInitialization: true);
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
                            await this.WaitAsync(800);
                        }
                    }

                    // Wait a bit before starting.
                    await this.WaitAsync(200);
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
                    await this.WaitAsync(200);
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

        return this.GetWindowPositionCore();
    }

    public IScreenshotContent GetCurrentWindowScreenshot()
    {
        return this.GetCurrentWindowScreenshot(false);
    }

    public void PressKey(AbstractWindowsEnvironment.VirtualKey key)
    {
        this.CancellationToken.ThrowIfCancellationRequested();

        // Check if the window is still active and in foreground.
        this.GetWindowPositionCore(failIfMinimized: !this.backgroundMode);

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
        this.GetWindowPositionCore(failIfMinimized: !this.backgroundMode);

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
            var pos = this.GetWindowPositionCore();

            // Convert the relative coordinates to absolute ones, then simulate the click.
            var absoluteCoords = pos.RelativeToAbsoluteCoordinates(c);
            this.environmentInterface.MoveMouse(
                checked((int)MathF.Round(absoluteCoords.X)),
                checked((int)MathF.Round(absoluteCoords.Y)));

            this.lastSetMouseCoordinates = absoluteCoords;
        }
        else
        {
            this.environmentInterface.MoveWindowMouse(
                this.windowHandle,
                checked((int)MathF.Round(c.X)),
                checked((int)MathF.Round(c.Y)),
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
            this.GetWindowPositionCore();

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
                    checked((int)MathF.Round(this.lastSetMouseCoordinates.Value.X)),
                    checked((int)MathF.Round(this.lastSetMouseCoordinates.Value.Y)));

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
                    checked((int)MathF.Round(this.lastSetMouseCoordinates.Value.X)),
                    checked((int)MathF.Round(this.lastSetMouseCoordinates.Value.Y)));
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
                        checked((int)MathF.Round(this.lastSetMouseCoordinates!.Value.X)),
                        checked((int)MathF.Round(this.lastSetMouseCoordinates.Value.Y)));
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
                this.GetWindowPositionCore(failIfMinimized: !this.backgroundMode);

                long remaining = milliseconds - sw.ElapsedMilliseconds;
                if (remaining <= 0)
                    break;

                await Task.Delay(
                    Math.Min((int)remaining, 100),
                    this.CancellationToken);
            }
        }
    }

    private WindowPosition GetWindowPositionCore(bool failIfMinimized = true)
    {
        // Fail if the window is no longer in foreground (active) and we are not
        // using background mode.
        var windowPosition = this.environmentInterface.GetWindowPosition(
            this.windowHandle,
            out _,
            failIfNotInForeground: !this.backgroundMode,
            failIfMinimized: failIfMinimized);

        // When we use mouse input or capture screenshots, check that the aspect
        // ratio of the window is 4:3 or higher if the window currently isn't minimized.
        if ((this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.MouseInput) ||
            this.simulator.RequiredCapabilities.IsSet(SimulatorCapabilities.CaptureScreenshot)) &&
            !windowPosition.IsMinimized)
        {
            if (((double)windowPosition.Size.Width / windowPosition.Size.Height) < 4d / 3d)
            {
                throw new ArgumentException(
                    "The Toontown window must have an aspect ratio " +
                    "of 4:3 or higher (e.g. 16:9).");
            }

            // TODO: Check if the window is beyond the virtual screen size (if in non-background
            // mode and we require mouse or screenshot capabilities, or if in background mode and
            // we require screenshot capabilities).
        }

        return windowPosition;
    }

    private IScreenshotContent GetCurrentWindowScreenshot(bool isInitialization)
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

        return this.currentScreenshot;
    }

    private async ValueTask CheckRetryForExceptionAsync(Exception ex, bool reinitialize)
    {
        if (!this.canRetryOnException || this.simulator.AsyncRetryHandler is null)
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