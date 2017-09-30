using System;

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
                    return this.r;
                case 1:
                    return this.g;
                case 2:
                    return this.b;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public System.Windows.Media.Color ToColor() => System.Windows.Media.Color.FromArgb(255, this.r, this.g, this.b);
    }
}
