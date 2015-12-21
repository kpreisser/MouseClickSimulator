using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
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
            // Note: The mouse coordinates are relative to the primary monitor size and
            // location, not to the virtual screen size, so we use
            // SystemInformation.PrimaryMonitorSize.
            var primaryScreenSize = System.Windows.Forms.SystemInformation.PrimaryMonitorSize;
            
            double x = 65536d * screenCoords.X / primaryScreenSize.Width;
            double y = 65536d * screenCoords.Y / primaryScreenSize.Height;

            /* For correct conversion when converting the flointing point numbers
               to integers, we need round away from 0, e.g.
               if x = 0, res = 0
               if  0 < x ≤ 1, res =  1
               if -1 ≤ x < 0, res = -1

               E.g. if a second monitor is placed at the left hand side of the primary monitor
               and both monitors have a resolution of 1280x960, the x-coordinates of the second
               monitor would be in the range (-1280, -1) and the ones of the primary monitor
               in the range (0, 1279).
               If we would want to place the mouse cursor at the rightmost pixel of the second
               monitor, we would calculate -1 / 1280 * 65536 = -51.2 and round that down to
               -52 which results in the screen x-coordinate of -1 (whereas -51 would result in 0).
               Similarly, +52 results in +1 whereas +51 would result in 0.
               Also, to place the cursor on the leftmost pixel on the second monitor we would use
               -65536 as mouse coordinates resulting in a screen x-coordinate of -1280 (whereas
               -65535 would result in -1279).
            */
            int resX = (int)(x >= 0 ? Math.Ceiling(x) : Math.Floor(x));
            int resY = (int)(y >= 0 ? Math.Ceiling(y) : Math.Floor(y));

            return new Coordinates(resX, resY);
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



        private unsafe class ScreenshotContent : IScreenshotContent
        {

            private bool disposed;

            private readonly Bitmap bmp;
            private readonly BitmapData bmpData;
            private readonly int* scan0;


            public Size Size
            {
                get { return new Size(bmp.Width, bmp.Height); }
            }

            public WindowPosition WindowPosition { get; }

            public ScreenshotContent(WindowPosition pos)
            {
                WindowPosition = pos;

                // Ensure we use Little Endian as byte order.
                // TODO: Is there a better way than using IPAddress to check this?
                if (IPAddress.HostToNetworkOrder((short)1) == 1)
                    throw new InvalidOperationException("This class currently only works "
                        + "on systems using little endian as byte order.");

                Rectangle rect = new Rectangle(
                    pos.Coordinates.X, pos.Coordinates.Y, pos.Size.Width, pos.Size.Height);

                bmp = new Bitmap(rect.Width, rect.Height,
                    PixelFormat.Format32bppRgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Location, new Point(0, 0),
                        rect.Size, CopyPixelOperation.SourceCopy);
                }
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly, bmp.PixelFormat);

                // Use unsafe mode for fast access to the bitmapdata. We use a int* 
                // pointer for faster access as the image format is 32-bit (althouth if
                // the pointer is not 32-bit aligned it might take two read operations).
                scan0 = (int*)bmpData.Scan0.ToPointer();
            }

            public ScreenshotColor GetPixel(Coordinates coords)
            {
                return GetPixel(coords.X, coords.Y);
            }

            public unsafe ScreenshotColor GetPixel(int x, int y)
            {
                // Only do these checks in Debug mode so we get optimal performance
                // when building as Release.
#if DEBUG
                if (disposed)
                    throw new ObjectDisposedException("ScreenshotContent");
                if (x < 0 || x >= bmp.Width)
                    throw new ArgumentOutOfRangeException(nameof(x));
                if (y < 0 || y >= bmp.Height)
                    throw new ArgumentOutOfRangeException(nameof(y));

                // This method assumes a 32-bit pixel format.
                if (bmpData.PixelFormat != PixelFormat.Format32bppRgb)
                    throw new InvalidOperationException("This method only works with a " 
                        + "pixel format of Format32bppRgb.");
#endif

                // Go to the line and the column. We use a int pointer to do a single
                // 32-Bit read instead of separate 8-Bit reads. We assume the runtime can
                // then hold the color variable in a register.
                int* ptr = scan0 + (y * bmpData.Width + x);
                int color = *ptr;

                return new ScreenshotColor()
                {
                    b = (byte)(color & 0xFF),
                    g = (byte)((color >> 0x8) & 0xFF),
                    r = (byte)((color >> 0x10) & 0xFF)
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
