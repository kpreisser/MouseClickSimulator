namespace TTMouseclickSimulator.Core.Environment
{
    
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

        
        public Coordinates RelativeToAbsoluteCoordinates(Coordinates c) => Coordinates.Add(c);
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

        public Coordinates Add(Coordinates c) => new Coordinates(X + c.X, Y + c.Y);
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
