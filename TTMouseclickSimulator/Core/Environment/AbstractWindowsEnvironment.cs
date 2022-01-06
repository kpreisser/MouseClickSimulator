using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace TTMouseclickSimulator.Core.Environment
{
    /// <summary>
    /// Provides methods to interact with other processes windows.
    /// </summary>
    public abstract class AbstractWindowsEnvironment
    {
        /// <summary>
        /// Finds processes with the specified name (without ".exe").
        /// </summary>
        /// <param name="processname"></param>
        /// <returns></returns>
        protected static List<Process> FindProcessesByName(string processname)
        {
            var processes = Process.GetProcessesByName(processname);
            var foundProcesses = new List<Process>();

            // Use the first applicable process.
            foreach (var p in processes)
            {
                try
                {
                    // Check if we actually have access to this process. This can fail with a
                    // Win32Exception e.g. if the process is from another user.
                    GC.KeepAlive(p.HasExited);
                    foundProcesses.Add(p);
                }
                catch
                {
                    // We cannot access the process, so dispose of it since we
                    // won't use it.
                    p.Dispose();
                }
            }

            return foundProcesses;
        }

        public abstract List<Process> FindProcesses();

        /// <summary>
        /// Finds the main window of the given process and returns its window handle.
        /// </summary>
        /// <param name="processname"></param>
        /// <exception cref="System.Exception"></exception>
        /// <returns></returns>
        public IntPtr FindMainWindowHandleOfProcess(Process p)
        {
            p.Refresh();
            if (p.HasExited)
                throw new ArgumentException("The process has exited.");

            var hWnd = p.MainWindowHandle;
            if (hWnd == IntPtr.Zero)
                throw new ArgumentException("Could not find Main Window.");

            return hWnd;
        }

        public void BringWindowToForeground(IntPtr hWnd)
        {
            if (!NativeMethods.SetForegroundWindow(hWnd))
                throw new Exception("Could not bring specified window to foreground.");
        }

        /// <summary>
        /// Determines the position and location of the client rectangle of the specified
        /// window. This method also checks if the specified window is in foreground.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public unsafe WindowPosition GetWindowPosition(
            IntPtr hWnd,
            out bool isInForeground,
            bool failIfNotInForeground = true,
            bool failIfMinimized = true)
        {
            // Note: To always correctly get the window position, the application must be
            // per-monitor (V1 or V2) DPI aware. Otherwise, we would get altered
            // location/size values when the target window is on a monitor that uses a
            // different DPI factor than the one for which the app is scaled.

            // Check if the specified window is in foreground.
            isInForeground = NativeMethods.GetForegroundWindow() == hWnd;
            if (failIfNotInForeground && !isInForeground)
                throw new Exception("The window is not in foreground any more.");

            // Get the client size.
            var clientRect = default(NativeMethods.RECT);
            if (!NativeMethods.GetClientRect(hWnd, &clientRect))
                throw new Win32Exception();

            // Get the screen coordinates of the point (0, 0) in the client rect.
            var relPos = default(NativeMethods.POINT);
            if (!NativeMethods.ClientToScreen(hWnd, &relPos))
                throw new Exception("Could not retrieve window client coordinates");

            var pos = new WindowPosition()
            {
                Coordinates = new Coordinates(relPos.x, relPos.y),
                Size = new Size(clientRect.right, clientRect.bottom)
            };

            // Check if the window is minimized.
            if (failIfMinimized && pos.IsMinimized)
                throw new Exception("The window has been minimized.");

            // Validate the position.
            this.ValidateWindowPosition(pos);
            return pos;
        }

        /// <summary>
        /// When overridden in subclasses, throws an exception if the window position is
        /// not valid. This implementation does nothing.
        /// </summary>
        /// <param name="pos">The WindowPosition to validate.</param>
        protected virtual void ValidateWindowPosition(WindowPosition pos)
        {
            // Do nothing.
        }

        public void CreateWindowScreenshot(
            IntPtr hWnd,
            ref ScreenshotContent existingScreenshot,
            bool failIfNotInForeground = true,
            bool fromScreen = false)
        {
            bool isInForeground;
            ScreenshotContent.Create(
                fromScreen ? IntPtr.Zero : hWnd,
                this.GetWindowPosition(hWnd, out isInForeground, failIfNotInForeground),
                ref existingScreenshot);
        }

        public void MoveMouse(int x, int y)
        {
            this.DoMouseInput(x, y, true, null);
        }

        public void PressMouseButton()
        {
            this.DoMouseInput(0, 0, false, true);
        }

        public void ReleaseMouseButton()
        {
            this.DoMouseInput(0, 0, false, false);
        }

        private void DoMouseInput(int x, int y, bool absoluteCoordinates, bool? mouseDown)
        {
            // Convert the screen coordinates into mouse coordinates.
            var cs = new Coordinates(x, y);
            cs = this.GetMouseCoordinatesFromScreenCoordinates(cs);

            var mi = new NativeMethods.MOUSEINPUT()
            {
                dx = cs.X,
                dy = cs.Y
            };

            if (absoluteCoordinates)
                mi.dwFlags |= NativeMethods.MOUSEEVENTF.ABSOLUTE;

            if (!(!absoluteCoordinates && x == 0 && y == 0))
            {
                // A movement occured.
                mi.dwFlags |= NativeMethods.MOUSEEVENTF.MOVE;
            }

            if (mouseDown.HasValue)
            {
                mi.dwFlags |= mouseDown.Value ?
                    NativeMethods.MOUSEEVENTF.LEFTDOWN :
                    NativeMethods.MOUSEEVENTF.LEFTUP;
            }

            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.InputType.INPUT_MOUSE,
                InputUnion = {
                    mi = mi
                }
            };

            NativeMethods.SendInput(input);
        }

        private Coordinates GetMouseCoordinatesFromScreenCoordinates(Coordinates screenCoords)
        {
            // Note: The mouse coordinates are relative to the primary monitor size and
            // location, not to the virtual screen size, so we use
            // SystemInformation.PrimaryMonitorSize.
            var primaryScreenSize = SystemInformation.PrimaryMonitorSize;

            double x = (double)0x10000 * screenCoords.X / primaryScreenSize.Width;
            double y = (double)0x10000 * screenCoords.Y / primaryScreenSize.Height;

            /* For correct conversion when converting the flointing point numbers
             * to integers, we need round away from 0, e.g.
             * if x = 0, res = 0
             * if  0 < x ≤ 1, res =  1
             * if -1 ≤ x < 0, res = -1
             *
             * E.g. if a second monitor is placed at the left hand side of the primary monitor
             * and both monitors have a resolution of 1280x960, the x-coordinates of the second
             * monitor would be in the range (-1280, -1) and the ones of the primary monitor
             * in the range (0, 1279).
             * If we would want to place the mouse cursor at the rightmost pixel of the second
             * monitor, we would calculate -1 / 1280 * 65536 = -51.2 and round that down to
             * -52 which results in the screen x-coordinate of -1 (whereas -51 would result in 0).
             * Similarly, +52 results in +1 whereas +51 would result in 0.
             * Also, to place the cursor on the leftmost pixel on the second monitor we would use
             * -65536 as mouse coordinates resulting in a screen x-coordinate of -1280 (whereas
             * -65535 would result in -1279).
             */
            int resX = checked((int)(x >= 0 ? Math.Ceiling(x) : Math.Floor(x)));
            int resY = checked((int)(y >= 0 ? Math.Ceiling(y) : Math.Floor(y)));

            return new Coordinates(resX, resY);
        }

        public Coordinates GetCurrentMousePosition()
        {
            var cursorPosition = Cursor.Position;
            return new Coordinates(cursorPosition.X, cursorPosition.Y);
        }

        public void PressKey(VirtualKey keyCode)
        {
            this.PressOrReleaseKey(keyCode, true);
        }

        public void ReleaseKey(VirtualKey keyCode)
        {
            this.PressOrReleaseKey(keyCode, false);
        }

        private void PressOrReleaseKey(VirtualKey keyCode, bool down)
        {
            var ki = new NativeMethods.KEYBDINPUT
            {
                wVk = keyCode
            };

            if (!down)
                ki.dwFlags = NativeMethods.KEYEVENTF.KEYUP;

            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.InputType.INPUT_KEYBOARD,
                InputUnion =
                {
                    ki = ki
                }
            };

            NativeMethods.SendInput(input);
        }

        public void WriteText(string characters)
        {
            var inputs = new NativeMethods.INPUT[2 * characters.Length];

            for (int i = 0; i < inputs.Length; i++)
            {
                var ki = new NativeMethods.KEYBDINPUT
                {
                    dwFlags = NativeMethods.KEYEVENTF.UNICODE,
                    wScan = characters[i / 2]
                };

                if (i % 2 == 1)
                    ki.dwFlags |= NativeMethods.KEYEVENTF.KEYUP;

                var input = new NativeMethods.INPUT
                {
                    type = NativeMethods.InputType.INPUT_KEYBOARD,
                    InputUnion =
                    {
                        ki = ki
                    }
                };

                inputs[i] = input;
            }

            NativeMethods.SendInputs(inputs);
        }

        public void MoveWindowMouse(IntPtr hWnd, int x, int y, bool isButtonDown)
        {
            NativeMethods.MK flags = 0;

            if (isButtonDown)
                flags |= NativeMethods.MK.LBUTTON;

            // We only send WM_LBUTTON and WM_LBUTTONUP messages, but no WM_MOUSEMOVE messages,
            // as in the latter case, the OS would send a WM_MOUSELEAVE message shortly
            // afterwards if the window is currently inactive, which would cause the operation
            // to not work correctly.
            if (isButtonDown)
            {
                this.SendWindowMouseMessage(hWnd, NativeMethods.WM.LBUTTONDOWN, x, y, flags);
            }
        }

        public void PressWindowMouseButton(IntPtr hWnd, int x, int y)
        {
            this.SendWindowMouseMessage(
                hWnd,
                NativeMethods.WM.LBUTTONDOWN,
                x,
                y,
                NativeMethods.MK.LBUTTON);
        }

        public void ReleaseWindowMouseButton(IntPtr hWnd, int x, int y)
        {
            // Note: There should be a short delay between calling PressWindowMouseButton
            // and ReleaseWindowMouseBUtton.
            this.SendWindowMouseMessage(hWnd, NativeMethods.WM.LBUTTONUP, x, y, 0);
        }

        private void SendWindowMouseMessage(
            IntPtr hWnd,
            NativeMethods.WM msg,
            int x,
            int y,
            NativeMethods.MK flags)
        {
            ushort x16 = checked((ushort)x);
            ushort y16 = checked((ushort)y);

            int wParam = (int)flags;
            int lParam = unchecked(x16 | (y16 << 16));

            // Use SendMessage rather than PostMessage so that we wait until the target
            // window has processed the message.
            NativeMethods.SendMessageManaged(
                hWnd,
                msg,
                (IntPtr)wParam,
                (IntPtr)lParam);
        }

        public void PressWindowKey(IntPtr hWnd, VirtualKey keyCode)
        {
            this.PressOrReleaseWindowKey(hWnd, keyCode, true);
        }

        public void ReleaseWindowKey(IntPtr hWnd, VirtualKey keyCode)
        {
            this.PressOrReleaseWindowKey(hWnd, keyCode, false);
        }

        private void PressOrReleaseWindowKey(IntPtr hWnd, VirtualKey keyCode, bool down)
        {
            var msg = down ? NativeMethods.WM.WM_KEYDOWN : NativeMethods.WM.WM_KEYUP;
            int wParam = (int)keyCode;
            int lParam = 1; // Bit 0-15: Repeat Count

            if (!down)
                lParam |= 3 << 30; // Previous key state (bit 30) and transition state (bit 31)

            if (keyCode == VirtualKey.Left || keyCode == VirtualKey.Right ||
                keyCode == VirtualKey.Up || keyCode == VirtualKey.Down ||
                keyCode == VirtualKey.Home || keyCode == VirtualKey.End)
            {
                // Specify the arrow keys as extended key (otherwise it would mean
                // they originate from the number pad).
                lParam |= 1 << 24;
            }

            // Use SendMessage rather than PostMessage so that we wait until the target
            // window has processed the message.
            NativeMethods.SendMessageManaged(hWnd, msg, (IntPtr)wParam, (IntPtr)lParam);
        }

        public void WriteWindowText(IntPtr hWnd, string characters)
        {
            foreach (char c in characters)
            {
                int wParam = c;
                int lParam = 1; // Bit 0-15: Repeat Count

                NativeMethods.SendMessageManaged(
                    hWnd,
                    NativeMethods.WM.WM_CHAR,
                    (IntPtr)wParam,
                    (IntPtr)lParam);
            }
        }

        public unsafe class ScreenshotContent : IScreenshotContent
        {
            private bool disposed;
            private WindowPosition windowPosition;
            private Rectangle rect;

            private readonly Bitmap bmp;
            private BitmapData bmpData;
            private int* scan0;

            private ScreenshotContent(WindowPosition pos)
            {
                // Ensure we use little endian as byte order (however, on Windows,
                // the endianness is always little endian).
                if (!BitConverter.IsLittleEndian)
                {
                    throw new InvalidOperationException(
                        "This class currently only works " +
                        "on systems using little endian as byte order.");
                }

                // Set the window position which will create a new rectangle.
                this.WindowPosition = pos;

                this.bmp = new Bitmap(
                    this.rect.Width,
                    this.rect.Height,
                    PixelFormat.Format32bppRgb);
            }

            public Size Size => new Size(this.bmp.Width, this.bmp.Height);

            public WindowPosition WindowPosition
            {
                get
                {
                    return this.windowPosition;
                }

                private set
                {
                    if (this.bmp != null && !(value.Size.Width == this.windowPosition.Size.Width &&
                        value.Size.Height == this.windowPosition.Size.Height))
                    {
                        throw new ArgumentException("Cannot set a new size for the same screenshot instance.");
                    }

                    this.windowPosition = value;

                    // Create a new rectangle for the new position
                    this.rect = new Rectangle(
                        this.windowPosition.Coordinates.X,
                        this.windowPosition.Coordinates.Y,
                        this.windowPosition.Size.Width,
                        this.windowPosition.Size.Height);
                }
            }

            public static void Create(
                    IntPtr windowHandle,
                    WindowPosition pos,
                    ref ScreenshotContent existingScreenshot)
            {
                // Try to reuse the existing screenshot's bitmap, if it has the same size.
                if (existingScreenshot != null &&
                    !(existingScreenshot.Size.Width == pos.Size.Width &&
                    existingScreenshot.Size.Height == pos.Size.Height))
                {
                    // We cannot use the existing screenshot, so dispose of it.
                    existingScreenshot.Dispose();
                    existingScreenshot = null;
                }

                if (existingScreenshot == null)
                {
                    existingScreenshot = new ScreenshotContent(pos);
                }
                else
                {
                    // The window could have been moved, so refresh the position.
                    existingScreenshot.WindowPosition = pos;
                }

                existingScreenshot.FillScreenshot(windowHandle);
            }

            private void OpenBitmapData()
            {
                if (this.bmpData == null)
                {
                    this.bmpData = this.bmp.LockBits(
                        new Rectangle(0, 0, this.bmp.Width, this.bmp.Height),
                        ImageLockMode.ReadOnly,
                        this.bmp.PixelFormat);

                    // Use unsafe mode for fast access to the bitmapdata. We use a int* 
                    // pointer for faster access as the image format is 32-bit (althouth if
                    // the pointer is not 32-bit aligned it might take two read operations).
                    this.scan0 = (int*)this.bmpData.Scan0.ToPointer();
                }
            }

            private void CloseBitmapData()
            {
                if (this.bmpData != null)
                {
                    this.bmp.UnlockBits(this.bmpData);
                    this.bmpData = null;
                    this.scan0 = null;
                }
            }

            private void FillScreenshot(IntPtr windowHandle)
            {
                this.CloseBitmapData();

                using (var g = Graphics.FromImage(this.bmp))
                {
                    if (windowHandle == IntPtr.Zero)
                    {
                        // Create the screenshot from the screen.
                        g.CopyFromScreen(
                            this.rect.Location,
                            new Point(0, 0),
                            this.rect.Size,
                            CopyPixelOperation.SourceCopy);
                    }
                    else
                    {
                        // Create the screenshot directly from the window's device context.
                        var windowClientDc = NativeMethods.GetDC(windowHandle);
                        if (windowClientDc == IntPtr.Zero)
                        {
                            throw new InvalidOperationException(
                                "Could not get window device context for creating a screenshot.");
                        }

                        try
                        {
                            var graphicsDc = g.GetHdc();

                            try
                            {
                                bool result = NativeMethods.BitBlt(
                                    graphicsDc,
                                    0,
                                    0,
                                    this.rect.Size.Width,
                                    this.rect.Size.Height,
                                    windowClientDc,
                                    0,
                                    0,
                                    (uint)CopyPixelOperation.SourceCopy);

                                if (!result)
                                    throw new Win32Exception();
                            }
                            finally
                            {
                                g.ReleaseHdc();
                            }
                        }
                        finally
                        {
                            NativeMethods.ReleaseDC(windowHandle, windowClientDc);
                        }
                    }
                }

                this.OpenBitmapData();
            }

            public ScreenshotColor GetPixel(Coordinates coords)
            {
                return this.GetPixel(coords.X, coords.Y);
            }

            public ScreenshotColor GetPixel(int x, int y)
            {
                // Only do these checks in Debug mode so we get optimal performance
                // when building as Release.
#if DEBUG
                if (this.disposed)
                    throw new ObjectDisposedException("ScreenshotContent");
                if (x < 0 || x >= this.bmp.Width)
                    throw new ArgumentOutOfRangeException(nameof(x));
                if (y < 0 || y >= this.bmp.Height)
                    throw new ArgumentOutOfRangeException(nameof(y));

                // This method assumes a 32-bit pixel format.
                if (this.bmpData.PixelFormat != PixelFormat.Format32bppRgb)
                    throw new InvalidOperationException(
                        "This method only works with a " +
                        "pixel format of Format32bppRgb.");
#endif

                // Go to the line and the column. We use a int pointer to do a single
                // 32-Bit read instead of separate 8-Bit reads. We assume the runtime can
                // then hold the color variable in a register.
                int* ptr = this.scan0 + (y * this.bmpData.Width + x);
                int color = *ptr;

                return new ScreenshotColor()
                {
                    b = (byte)(color & 0xFF),
                    g = (byte)((color >> 0x8) & 0xFF),
                    r = (byte)((color >> 0x10) & 0xFF)
                };
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposing)
            {
                if (!this.disposed && disposing)
                {
                    this.CloseBitmapData();
                    this.bmp.Dispose();
                }

                this.disposed = true;
            }
        }


        public enum VirtualKey : ushort
        {
            Enter = 0x0D,
            Control = 0x11,

            Left = 0x25,
            Up = 0x26,
            Right = 0x27,
            Down = 0x28,

            Home = 0x24,
            End = 0x23
        }
    }
}
