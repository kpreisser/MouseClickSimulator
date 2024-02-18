using System;
using System.Threading;

namespace TTMouseClickSimulator.Core.Environment;

/// <summary>
/// Allows actions to interact with the target window, e.g. press keys and
/// simulate mouse clicks.
/// </summary>
public interface IInteractionProvider
{
    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that indicates whether the simulator
    /// was cancelled.
    /// </summary>
    /// <value>
    /// A <see cref="CancellationToken"/> that indicates whether the simulator was
    /// cancelled.
    /// </value>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Checks whether the simulator should retry the currenct action after an
    /// exception occured.
    /// </summary>
    /// <remarks>
    /// If this method returns, this means the action should run again. Otherwise,
    /// this method will rethrow the exception or throw an
    /// <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <param name="ex"></param>
    void CheckRetryForException(Exception ex);

    /// <summary>
    /// Waits until the specified interval is elapsed or the simulator has been canceled.
    /// </summary>
    /// <param name="millisecondsTimeout">The interval to wait.</param>
    /// <param name="useAccurateTimer">Specifies whether to use an accurate timer. If
    /// <c>true</c>, measuring of the time is more accurate but requires a bit CPU usage
    /// shortly before the method returns.</param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException">The wait has been cancelled.
    /// IActions don't need to catch this exception.</exception>
    void Wait(int millisecondsTimeout, bool useAccurateTimer = false);

    /// <summary>
    /// Gets the current position of the destination window.
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException">The simulator has been cancelled.
    /// IActions don't need to catch this exception.</exception>
    WindowPosition GetCurrentWindowPosition();

    /// <summary>
    /// Gets a current screenshot of the window. Note that because the IInteractionProvider
    /// caches the current screenshot for performance reason, this method may return
    /// the same IScreenshotContent instance as previous calls but with refreshed content.
    /// </summary>
    /// <returns></returns>
    IScreenshotContent GetCurrentWindowScreenshot();

    /// <summary>
    /// Moves the mouse to the specified window-relative coordinates.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    void MoveMouse(int x, int y);

    /// <summary>
    /// Moves the mouse to the specified window-relative coordinates.
    /// </summary>
    /// <param name="c"></param>
    void MoveMouse(Coordinates c);

    void PressMouseButton();

    void ReleaseMouseButton();

    void PressKey(WindowsEnvironment.VirtualKey key);

    void ReleaseKey(WindowsEnvironment.VirtualKey key);

    /// <summary>
    /// Writes the given string into the window.
    /// </summary>
    /// <param name="text"></param>
    void WriteText(string text);
}
