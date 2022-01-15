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
    public (int X, int Y) Coordinates
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
        get => this.Coordinates.X is -32000 && this.Coordinates.Y is -32000 &&
            this.Size.Width is 0 && this.Size.Height is 0;
    }

    public Coordinates RelativeToAbsoluteCoordinates(Coordinates c)
    {
        return c.Add(this.Coordinates);
    }
}

public struct Coordinates
{
    public Coordinates(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }

    public float X
    {
        get;
        set;
    }

    public float Y
    {
        get;
        set;
    }

    public Coordinates Add(Coordinates c)
    {
        return new Coordinates(this.X + c.X, this.Y + c.Y);
    }

    public static implicit operator Coordinates((int X, int Y) intCoordinates)
    {
        return new Coordinates(intCoordinates.X, intCoordinates.Y);
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
