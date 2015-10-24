using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{

    public enum FishingSpotFlavor : int
    {
        PunchlinePlace = 0
    }

    public class FishingSpotFlavorData
    {

        private static readonly Dictionary<FishingSpotFlavor, FishingSpotFlavorData> elements
            = new Dictionary<FishingSpotFlavor, FishingSpotFlavorData>();

        static FishingSpotFlavorData()
        {
            elements.Add(FishingSpotFlavor.PunchlinePlace,
                new FishingSpotFlavorData(new Coordinates(260, 196), new Coordinates(1349, 626),
                new ScreenshotColor(22, 140, 116), 13));
        }

        public static FishingSpotFlavorData GetDataFromItem(FishingSpotFlavor item)
        {
            return elements[item];
        }



        public Coordinates Scan1 { get; }
        public Coordinates Scan2 { get; }
        public ScreenshotColor BubbleColor { get; }
        public int Tolerance { get; }


        public FishingSpotFlavorData(Coordinates scan1, Coordinates scan2,
            ScreenshotColor bubbleColor, int tolerance)
        {
            this.Scan1 = scan1;
            this.Scan2 = scan2;
            this.BubbleColor = bubbleColor;
            this.Tolerance = tolerance;
        }

    }
}
