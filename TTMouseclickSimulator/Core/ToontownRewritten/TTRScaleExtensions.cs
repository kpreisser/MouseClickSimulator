using System;

using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten
{
    public static class TTRScaleExtensions
    {
        /// <summary>
        /// Converts relative coordinates in the window to new absolute coordinates
        /// based on the specified reference size.
        /// Note: TT Rewritten scales its window contents accounting for the aspect ratio
        /// (at least if the width is greater than from the 4:3 resolution). Therefore
        /// we use the given VerticalScaleAlignment to correctly calculate the
        /// X coordinate (note that we require the aspect ratio to be ≥ 4:3).
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="coords">The coordinates to scale.</param>
        /// <param name="referenceSize">The size which was used to create the coordinates.
        /// This size is interpreted as being a scaled 4:3 window without retaining
        /// aspect ratio.</param>
        /// <param name="valign"></param>
        /// <returns></returns>
        public static Coordinates ScaleCoordinates(
                this WindowPosition pos,
                Coordinates coords, 
                Size referenceSize,
                VerticalScaleAlignment valign = VerticalScaleAlignment.Center)
        {
            double aspectWidth = pos.Size.Height / 3d * 4d;
            double widthDifference = pos.Size.Width - aspectWidth;

            int newX;
            if (valign == VerticalScaleAlignment.NoAspectRatio)
            {
                newX = (int)Math.Round((double)coords.X / referenceSize.Width * pos.Size.Width);
            }
            else
            {
                newX = (int)Math.Round((double)coords.X / referenceSize.Width * aspectWidth
                    + (valign == VerticalScaleAlignment.Left ? 0 : 
                    valign == VerticalScaleAlignment.Center ? widthDifference / 2 : widthDifference));
            }

            return new Coordinates()
            {
                X = newX,
                Y = (int)Math.Round((double)coords.Y / referenceSize.Height * pos.Size.Height)
            };
        }
    }


    /// <summary>
    /// Specifies how the given X-Coordinate should be scaled.
    /// </summary>
    public enum VerticalScaleAlignment
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
}
