using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.Actions
{
    internal class StandardInteractionProvider : IInteractionProvider, IDisposable
    {

        /// <summary>
        /// Specifies if this InteractionProvider has been canceled. This flag can be set by
        /// another thread.
        /// </summary>
        private volatile bool isCanceled = false;

        private readonly SemaphoreSlim waitSemaphore = new SemaphoreSlim(0);

        private readonly AbstractEnvironmentInterface environmentInterface;

        private Process process;
        private bool isMouseButtonPressed = false;
        private List<AbstractEnvironmentInterface.VirtualKeyShort> keysCurrentlyPressed 
            = new List<AbstractEnvironmentInterface.VirtualKeyShort>();

        public StandardInteractionProvider(AbstractEnvironmentInterface environmentInterface)
        {
            this.environmentInterface = environmentInterface;
        }

        public void Initialize()
        {
            process = environmentInterface.FindProcess();

            // Bring the destination window to foreground.
            IntPtr hWnd = environmentInterface.FindMainWindowHandleOfProcess(process);
            environmentInterface.BringWindowToForeground(hWnd);
        }

        /// <summary>
        /// Checks that the InteractionProvider has not been canceled and that the main
        /// window is still active.
        /// </summary>
        private void EnsureNotCanceled()
        {
            if (isCanceled)
                throw new ActionCanceledException();
        }

        public async Task WaitAsync(int millisecondsTimeout)
        {
            EnsureNotCanceled();
            await waitSemaphore.WaitAsync(millisecondsTimeout);
            EnsureNotCanceled();
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

        public AbstractEnvironmentInterface.ScreenshotContent CreateCurrentWindowScreenshot()
        {
            EnsureNotCanceled();

            IntPtr hWnd = environmentInterface.FindMainWindowHandleOfProcess(process);
            return environmentInterface.CreateWindowScreenshot(hWnd);
        }

        public void PressKey(AbstractEnvironmentInterface.VirtualKeyShort key)
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

        public void ReleaseKey(AbstractEnvironmentInterface.VirtualKeyShort key)
        {
            EnsureNotCanceled();

            int kcpIdx = keysCurrentlyPressed.IndexOf(key);
            if (kcpIdx >= 0)
            {
                environmentInterface.ReleaseKey(key);
                keysCurrentlyPressed.RemoveAt(kcpIdx);
            }
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
        /// Cancels this StandardInteractionProvider. When the thread which calls the methods
        /// is a WPF GUI thread (which means async tasks are continued in the GUI thread when
        /// awaiting them), this method may be called while a task is currently waiting in the
        /// WaitAsync() method.
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing && !isCanceled)
            {
                isCanceled = true;

                // Release the semaphore (so that a task that is waiting can continue), then
                // dispose it.
                waitSemaphore.Release();
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

                foreach (AbstractEnvironmentInterface.VirtualKeyShort key in keysCurrentlyPressed)
                {
                    environmentInterface.ReleaseKey(key);
                }
            }
        }
    }
}
