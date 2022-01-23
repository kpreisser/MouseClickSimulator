using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown;

public static class TTScaleExtensions
{
    /// <summary>
    /// Converts window coordinates for the specified <paramref name="referenceSize"/> to
    /// coordinates for the actual window size.
    /// </summary>
    /// <remarks>
    /// Note: TT Rewritten and Corporate Clash scale their window contents accounting for
    /// the aspect ratio (at least if the width is greater than from the 4:3 resolution).
    /// Therefore we use the given VerticalScaleAlignment to correctly calculate the
    /// X coordinate (note that we require the aspect ratio to be ≥ 4:3).
    /// </remarks>
    /// <param name="pos"></param>
    /// <param name="coords">The coordinates to scale.</param>
    /// <param name="referenceSize">The size which was used to create the coordinates.
    /// This size is interpreted as being a scaled 4:3 window without retaining
    /// aspect ratio.</param>
    /// <param name="align"></param>
    /// <returns></returns>
    public static Coordinates ScaleCoordinates(
            this WindowPosition pos,
            Coordinates coords,
            Size referenceSize,
            HorizontalScaleAlignment align = HorizontalScaleAlignment.Center)
    {
        double aspectWidth = pos.Size.Height / 3d * 4d;
        double widthDifference = pos.Size.Width - aspectWidth;

        float newX;
        if (align is HorizontalScaleAlignment.NoAspectRatio)
        {
            newX = (float)((double)coords.X / referenceSize.Width * pos.Size.Width);
        }
        else
        {
            newX = (float)((double)coords.X / referenceSize.Width * aspectWidth +
                widthDifference * align switch
                {
                    HorizontalScaleAlignment.Left => 0,
                    HorizontalScaleAlignment.Center => 0.5,
                    _ => 1
                });
        }

        return new Coordinates()
        {
            X = newX,
            Y = (float)((double)coords.Y / referenceSize.Height * pos.Size.Height)
        };
    }
}

/// <summary>
/// Specifies how the given X coordinate should be scaled.
/// </summary>
public enum HorizontalScaleAlignment
{
    Left,
    Center,
    Right,
    /// <summary>
    /// Specifies that the coordinate should be scaled using the complete
    /// window width, not the width from the 4:3 aspect ratio.
    /// </summary>
    NoAspectRatio
}
