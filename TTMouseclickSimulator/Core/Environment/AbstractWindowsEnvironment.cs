using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Environment
{
    /// <summary>
    /// Provides methods to interact with other processes windows.
    /// </summary>
    public abstract class AbstractWindowsEnvironment
    {

        /// <summary>
        /// Finds the process with the specified name (without ".exe").
        /// </summary>
        /// <param name="processname"></param>
        /// <returns></returns>
        protected Process FindProcessByName(string processname)
        {
            Process[] processes = Process.GetProcessesByName(processname);
            if (processes.Length == 0)
                throw new ArgumentException($"Could not find Process '{processname}'.");

            return processes[0];

        }
        /// <summary>
        /// Finds the main window of the process with the specified name (without ".exe") 
        /// and returns its main window handle.
        /// </summary>
        /// <param name="processname"></param>
        /// <exception cref="System.Exception"></exception>
        /// <returns></returns>
        public IntPtr FindMainWindowHandleOfProcess(Process p)
        {
            p.Refresh();
            if (p.HasExited)
                throw new ArgumentException("The process has exited.");

            IntPtr hWnd = p.MainWindowHandle;
            if (hWnd == IntPtr.Zero)
                throw new ArgumentException("Could not find Main Window.");

            return hWnd;
        }

        public void BringWindowToForeground(IntPtr hWnd)
        {
            if (!NativeMethods.SetForegroundWindow(hWnd))
                throw new Exception("Could not bring specified window to foreground.");
        }

        public abstract Process FindProcess();

        /// <summary>
        /// Determines the position and location of the client rectangle of the specified
        /// window. This method also checks if the specified window is in foreground.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public WindowPosition GetWindowPosition(IntPtr hWnd)
        {
            // Check if the specified window is in foreground.
            if (NativeMethods.GetForegroundWindow() != hWnd)
                throw new Exception("The window is not in foreground any more.");

            // TODO: Need to check if the calculaction done here is correct, especially with
            // different screen DPI settings and multiple monitors.
            // Get the client size.
            NativeMethods.RECT clientRect;
            if (!NativeMethods.GetClientRect(hWnd, out clientRect))
                throw new System.ComponentModel.Win32Exception();

            // Get the screen coordinates of the point (0, 0) in the client rect.
            NativeMethods.POINT relPos = new NativeMethods.POINT();
            if (!NativeMethods.ClientToScreen(hWnd, ref relPos))
                throw new Exception("Could not retrieve window client coordinates");

            // Check if the window is minimized.
            if (clientRect.Bottom - clientRect.Top == 0 && clientRect.Right - clientRect.Left == 0
                && relPos.X == -32000 && relPos.Y == -32000)
                throw new Exception("The window has been minimized.");


            var pos = new WindowPosition()
            {
                Coordinates = new Coordinates(relPos.X, relPos.Y),
                Size = new Size(clientRect.Right - clientRect.Left, clientRect.Bottom - clientRect.Top)
            };
            // Validate the position.
            ValidateWindowPosition(pos);
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

        public IScreenshotContent CreateWindowScreenshot(IntPtr hWnd)
        {
            WindowPosition pos = GetWindowPosition(hWnd);
            ScreenshotContent scrn = new ScreenshotContent(pos);
            return scrn;
        }


        public void MoveMouse(int x, int y)
        {
            DoMouseInput(x, y, true, null);
        }

        public void PressMouseButton()
        {
            DoMouseInput(0, 0, false, true);
        }

        public void ReleaseMouseButton()
        {
            DoMouseInput(0, 0, false, false);
        }

        private void DoMouseInput(int x, int y, bool absoluteCoordinates, bool? mouseDown)
        {
            // TODO: Maybe we should instead send WM_MOUSEMOVE, WM_LBUTTONDOWN etc.
            // messages directly to the destination window so that we don't need to
            // position the mouse cursor which makes it harder e.g. to
            // click on the "Stop" button of the simulator.

            // Convert the screen coordinates into mouse coordinates.
            Coordinates cs = new Coordinates(x, y);
            cs = GetMouseCoordinatesFromScreenCoordinates(cs);

            var mi = new NativeMethods.MOUSEINPUT();
            mi.dx = cs.X;
            mi.dy = cs.Y;
            if (absoluteCoordinates)
                mi.dwFlags |= NativeMethods.MOUSEEVENTF.ABSOLUTE;
            if (!(!absoluteCoordinates && x == 0 && y == 0))
            {
                // A movement occured.
                mi.dwFlags |= NativeMethods.MOUSEEVENTF.MOVE;
            }

            if (mouseDown.HasValue)
            {
                mi.dwFlags |= mouseDown.Value ? NativeMethods.MOUSEEVENTF.LEFTDOWN 
                    : NativeMethods.MOUSEEVENTF.LEFTUP;
            }
            
            var input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_MOUSE;
            input.U.mi = mi;

            NativeMethods.INPUT[] inputs = { input };

            if (NativeMethods.SendInput(1, inputs, NativeMethods.INPUT.Size) == 0)
                throw new System.ComponentModel.Win32Exception();
        }

        private Coordinates GetMouseCoordinatesFromScreenCoordinates(Coordinates screenCoords)
        {
            // Note: The mouse coordinates seems to be relative to the primary monitor size and
            // location, not to the virtual screen size. Therefore we use the PrimaryMonitorSize.
            var primaryScreenSize = System.Windows.Forms.SystemInformation.PrimaryMonitorSize;
            int x = (int)Math.Ceiling((((double)screenCoords.X) * 65536) 
                / primaryScreenSize.Width);
            int y = (int)Math.Ceiling((((double)screenCoords.Y) * 65536) 
                / primaryScreenSize.Height);

            return new Coordinates(x, y);
        }


        public void PressKey(VirtualKeyShort keyCode)
        {
            PressOrReleaseKey(keyCode, true);
        }

        public void ReleaseKey(VirtualKeyShort keyCode)
        {
            PressOrReleaseKey(keyCode, false);
        }

        private void PressOrReleaseKey(VirtualKeyShort keyCode, bool down)
        {
            var ki = new NativeMethods.KEYBDINPUT();
            ki.wVk = keyCode;
            if (!down)
                ki.dwFlags = NativeMethods.KEYEVENTF.KEYUP;

            var input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.U.ki = ki;

            NativeMethods.INPUT[] inputs = { input };

            if (NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.INPUT.Size) == 0)
                throw new System.ComponentModel.Win32Exception();
        }

        public void WriteText(string characters)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[2 * characters.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                var ki = new NativeMethods.KEYBDINPUT();
                ki.dwFlags = NativeMethods.KEYEVENTF.UNICODE;
                if (i % 2 == 1)
                    ki.dwFlags |= NativeMethods.KEYEVENTF.KEYUP;
                ki.wScan = (short)characters[i / 2];

                var input = new NativeMethods.INPUT();
                input.type = NativeMethods.INPUT_KEYBOARD;
                input.U.ki = ki;

                inputs[i] = input;
            }

            if (NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.INPUT.Size) == 0)
                throw new System.ComponentModel.Win32Exception();

        }



        private class ScreenshotContent : IScreenshotContent
        {

            private bool disposed;

            private readonly System.Drawing.Bitmap bmp;
            private readonly System.Drawing.Imaging.BitmapData bmpData;


            public Size Size
            {
                get { return new Size(bmp.Width, bmp.Height); }
            }

            public WindowPosition WindowPosition { get; }

            public ScreenshotContent(WindowPosition pos)
            {
                WindowPosition = pos;

                Rectangle rect = new Rectangle(
                    pos.Coordinates.X, pos.Coordinates.Y, pos.Size.Width, pos.Size.Height);

                bmp = new Bitmap(rect.Width, rect.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Location, new System.Drawing.Point(0, 0),
                        rect.Size, CopyPixelOperation.SourceCopy);
                }
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            }

            public ScreenshotColor GetPixel(Coordinates coords)
            {
                return GetPixel(coords.X, coords.Y);
            }

            public unsafe ScreenshotColor GetPixel(int x, int y)
            {
                if (disposed)
                    throw new ObjectDisposedException("ScreenshotContent");
                if (x < 0 || x >= bmp.Width)
                    throw new ArgumentOutOfRangeException(nameof(x));
                if (y < 0 || y >= bmp.Height)
                    throw new ArgumentOutOfRangeException(nameof(y));

                // This method assumes a 32-bit pixel format.
                if (bmpData.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppRgb)
                    throw new InvalidOperationException("This method only works with a " 
                        + "pixel format of Format32bppRgb.");

                // Use unsafe mode for fast access to the bitmapdata.
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                // Go to the line
                ptr += y * bmpData.Stride;
                // Go to the column. 
                ptr += 4 * x;

                byte b = *(ptr + 0);
                byte g = *(ptr + 1);
                byte r = *(ptr + 2);

                return new ScreenshotColor()
                {
                    r = r,
                    g = g,
                    b = b
                };
            }


            ~ScreenshotContent()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            protected void Dispose(bool disposing)
            {
                if (!disposed && disposing)
                {
                    bmp.UnlockBits(bmpData);
                    bmp.Dispose();
                }
                disposed = true;
            }

        }

        

        public enum VirtualKeyShort : short
        {
            ///<summary>
            ///ENTER key
            ///</summary>
            Enter = 0x0D,

            ///<summary>
            ///CTRL key
            ///</summary>
            Control = 0x11,

            ///<summary>
            ///LEFT ARROW key
            ///</summary>
            Left = 0x25,
            ///<summary>
            ///UP ARROW key
            ///</summary>
            Up = 0x26,
            ///<summary>
            ///RIGHT ARROW key
            ///</summary>
            Right = 0x27,
            ///<summary>
            ///DOWN ARROW key
            ///</summary>
            Down = 0x28,



        }
    }
}
