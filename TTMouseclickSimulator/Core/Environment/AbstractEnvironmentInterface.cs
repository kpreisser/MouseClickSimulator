using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Environment
{
    /// <summary>
    /// Provides methods to interact with other processes windows.
    /// </summary>
    public abstract class AbstractEnvironmentInterface
    {
        /// <summary>
        /// Finds the main window of the process with the specified name and returns its parameters.
        /// </summary>
        /// <param name="processname"></param>
        /// <exception cref="System.Exception"></exception>
        /// <returns></returns>
        protected IntPtr FindMainWindowHandleOfProcess(string processname)
        {
            throw new NotImplementedException(); // TODO
        }

        public abstract IntPtr FindMainWindowHandle();

        public WindowPosition GetMainWindowPosition(IntPtr hWnd)
        {
            throw new NotImplementedException();
        }

        public ScreenshotContent GetMainWindowScreenshot(IntPtr hWnd)
        {
            throw new NotImplementedException();
        }


        public void MoveMouse(int x, int y)
        {
            throw new NotImplementedException();
        }

        public void PressMouseButton()
        {
            throw new NotImplementedException();
        }

        public void ReleaseMouseButton()
        {
            throw new NotImplementedException();
        }

        public void PressKey(VirtualKeyShort keyCode)
        {
            throw new NotImplementedException();
        }

        public void ReleaseKey(VirtualKeyShort keyCode)
        {
            throw new NotImplementedException();
        }




        public class ScreenshotContent : IDisposable
        {

            private bool disposed;

            private readonly System.Drawing.Bitmap bmp;
            private readonly System.Drawing.Imaging.BitmapData bmpData;


            public Size Size
            {
                get
                {
                    return new Size()
                    {
                        width = bmp.Width,
                        height = bmp.Height
                    };
                }
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

            public System.Windows.Media.Color ToColor()
            {
                return System.Windows.Media.Color.FromArgb(255, r, g, b);
            }
        }





        public enum VirtualKeyShort : short
        {
            ///<summary>
            ///Left mouse button
            ///</summary>
            LBUTTON = 0x01,
            ///<summary>
            ///Right mouse button
            ///</summary>
            RBUTTON = 0x02,

            ///<summary>
            ///ENTER key
            ///</summary>
            RETURN = 0x0D,

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
