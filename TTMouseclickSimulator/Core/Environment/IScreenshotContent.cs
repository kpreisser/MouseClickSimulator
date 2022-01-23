using System;
using System.Windows.Media;

namespace TTMouseClickSimulator.Core.Environment;

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
        return index switch
        {
            0 => this.r,
            1 => this.g,
            2 => this.b,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    public Color ToColor()
    {
        return Color.FromArgb(255, this.r, this.g, this.b);
    }
}
