using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Environment
{
    public interface IScreenshotContent
    {
        WindowPosition WindowPosition { get; }

        ScreenshotColor GetPixel(Coordinates coords);

        ScreenshotColor GetPixel(int x, int y);
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
            switch (index) {
                case 0:
                    return r;
                case 1:
                    return g;
                case 2:
                    return b;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public System.Windows.Media.Color ToColor() => System.Windows.Media.Color.FromArgb(255, r, g, b);
    }
}
