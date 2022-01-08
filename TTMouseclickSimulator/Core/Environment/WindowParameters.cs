namespace TTMouseclickSimulator.Core.Environment;

/// <summary>
/// Specifies parameters of the destination window in pixels.
/// Note that the window border is excluded.
/// </summary>
public struct WindowPosition
{
    /// <summary>
    /// The coordinates to the upper left point of the window contents.
    /// </summary>
    public Coordinates Coordinates
    {
        get;
        set;
    }

    /// <summary>
    /// The size of the window contents.
    /// </summary>
    public Size Size
    {
        get;
        set;
    }

    public bool IsMinimized
    {
        get => this.Coordinates.X == -32000 && this.Coordinates.Y == -32000 &&
            this.Size.Width == 0 && this.Size.Height == 0;
    }

    public Coordinates RelativeToAbsoluteCoordinates(Coordinates c)
    {
        return this.Coordinates.Add(c);
    }
}

public struct Coordinates
{
    public Coordinates(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public int X
    {
        get;
        set;
    }

    public int Y
    {
        get;
        set;
    }

    public Coordinates Add(Coordinates c)
    {
        return new Coordinates(this.X + c.X, this.Y + c.Y);
    }
}

public struct Size
{
    public Size(int width, int height)
    {
        this.Width = width;
        this.Height = height;
    }

    public int Width
    {
        get;
        set;
    }

    public int Height
    {
        get;
        set;
    }
}
