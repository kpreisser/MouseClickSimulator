using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Environment
{
    
    //public class WindowParameters
    //{
    //    public IntPtr hWnd;
    //    public WindowPosition windowPosition { get; set; }

    //}


    /// <summary>
    /// Specifies parameters of the destination window in pixels.
    /// Note that the window border is excluded.
    /// </summary>
    public struct WindowPosition
    {
        /// <summary>
        /// The coordinates to the upper left point of the window contents.
        /// </summary>
        public Coordinates Coordinates { get; set; }
        /// <summary>
        /// The size of the window contents.
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Converts coordinates in the window to new ones based on the
        /// specified size.
        /// </summary>
        /// <param name=""></param>
        /// <param name="previousSize"></param>
        /// <returns></returns>
        public Coordinates ConvertCoordinates(Coordinates coords, Size oldSize)
        {
            return new Coordinates()
            {
                X = (int)Math.Round((double)coords.X / oldSize.Width * Size.Width),
                Y = (int)Math.Round((double)coords.Y / oldSize.Height * Size.Height)
            };
        }
    }

    public struct Coordinates
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinates(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public struct Size
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Size(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

    }

}
