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
        public Coordinates coordinates { get; set; }
        /// <summary>
        /// The size of the window contents.
        /// </summary>
        public Size size { get; set; }

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
                x = (int)Math.Round((double)coords.x / oldSize.width * size.width),
                y = (int)Math.Round((double)coords.y / oldSize.height * size.height)
            };
        }
    }

    public struct Coordinates
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public struct Size
    {
        public int width { get; set; }
        public int height { get; set; }
    }

}
