using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class FishingSpotFlavor
    {
        public Coordinates Scan1 { get; }
        public Coordinates Scan2 { get; }
        public ScreenshotColor BubbleColor { get; }
        public int Tolerance { get; }


        public FishingSpotFlavor(Coordinates scan1, Coordinates scan2,
            ScreenshotColor bubbleColor, int tolerance)
        {
            this.Scan1 = scan1;
            this.Scan2 = scan2;
            this.BubbleColor = bubbleColor;
            this.Tolerance = tolerance;
        }


        public static readonly FishingSpotFlavor PunchlinePlace = 
            new FishingSpotFlavor(new Coordinates(260, 196), new Coordinates(1349, 626), 
                new ScreenshotColor(22, 140, 116), 13);

    }
}
