using System;
using System.Collections.Generic;
using System.Diagnostics;
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


            return new WindowPosition()
            {
                Coordinates = new Coordinates(relPos.X, relPos.Y),
                Size = new Size(clientRect.Right - clientRect.Left, clientRect.Bottom - clientRect.Top)
            };
        }

        public ScreenshotContent CreateWindowScreenshot(IntPtr hWnd)
        {
            WindowPosition pos = GetWindowPosition(hWnd);
            ScreenshotContent scrn = new ScreenshotContent(new System.Drawing.Rectangle(
                pos.Coordinates.X, pos.Coordinates.Y, pos.Size.Width, pos.Size.Height));
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
                mi.dwFlags |= mouseDown.Value ? NativeMethods.MOUSEEVENTF.LEFTDOWN : NativeMethods.MOUSEEVENTF.LEFTUP;
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
            var virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;
            int x = (int)Math.Ceiling((((double)screenCoords.X - virtualScreen.Left) * 65536) / virtualScreen.Width);
            int y = (int)Math.Ceiling((((double)screenCoords.Y - virtualScreen.Top) * 65536) / virtualScreen.Height);

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

            if (NativeMethods.SendInput(1, inputs, NativeMethods.INPUT.Size) == 0)
                throw new System.ComponentModel.Win32Exception();
        }



        public class ScreenshotContent : IDisposable
        {

            private bool disposed;

            private readonly System.Drawing.Bitmap bmp;
            private readonly System.Drawing.Imaging.BitmapData bmpData;


            public Size Size
            {
                get { return new Size(bmp.Width, bmp.Height); }
            }

            public ScreenshotContent(System.Drawing.Rectangle rect)
            {
                bmp = new System.Drawing.Bitmap(rect.Width, rect.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Location, new System.Drawing.Point(0, 0),
                        rect.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                }
                bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
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
                    throw new InvalidOperationException("This method only works with a pixel format of Format32bppRgb.");

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

        public struct ScreenshotColor
        {
            public byte r;
            public byte g;
            public byte b;

            public ScreenshotColor(byte r, byte g, byte b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }

            public byte GetValueFromIndex(int index)
            {
                if (index == 0)
                    return r;
                else if (index == 1)
                    return g;
                else if (index == 2)
                    return b;

                throw new ArgumentOutOfRangeException(nameof(index));
            }

            public System.Windows.Media.Color ToColor()
            {
                return System.Windows.Media.Color.FromArgb(255, r, g, b);
            }
        }





        public enum VirtualKeyShort : short
        {
            ///<summary>
            ///ENTER key
            ///</summary>
            RETURN = 0x0D,

            ///<summary>
            ///CTRL key
            ///</summary>
            CONTROL = 0x11,

            ///<summary>
            ///LEFT ARROW key
            ///</summary>
            LEFT = 0x25,
            ///<summary>
            ///UP ARROW key
            ///</summary>
            UP = 0x26,
            ///<summary>
            ///RIGHT ARROW key
            ///</summary>
            RIGHT = 0x27,
            ///<summary>
            ///DOWN ARROW key
            ///</summary>
            DOWN = 0x28,



        }
    }
}
