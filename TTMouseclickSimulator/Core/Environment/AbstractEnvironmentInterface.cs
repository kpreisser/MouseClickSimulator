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

        public ScreenshotContents GetMainWindowScreenshot(IntPtr hWnd)
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




        public class ScreenshotContents
        {
            ScreenshotColor[,] pixels;
        }

        public struct ScreenshotColor
        {
            public int rgb;

            public System.Windows.Media.Color ToColor()
            {
                byte r = (byte)(rgb & 0xFF);
                byte g = (byte)((rgb >> 0x100) & 0xFF);
                byte b = (byte)((rgb >> 0x10000) & 0xFF);
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
