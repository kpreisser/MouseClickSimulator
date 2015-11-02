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
        PunchlinePlace = 0,
        LighthouseLane,

        // Halloween flavors
        PunchlinePlaceHalloween = 100
    }

    public class FishingSpotFlavorData
    {
        // TODO: We should allow the XML project file to specify these parameters so they
        // don't need to be hard-coded in the source code. That way users could add other fishing
        // places using the XML project file.

        private static readonly Dictionary<FishingSpotFlavor, FishingSpotFlavorData> elements
            = new Dictionary<FishingSpotFlavor, FishingSpotFlavorData>();

        static FishingSpotFlavorData()
        {
            elements.Add(FishingSpotFlavor.PunchlinePlace,
                new FishingSpotFlavorData(new Coordinates(260, 196), new Coordinates(1349, 626),
                new ScreenshotColor(22, 140, 116), 13));
            elements.Add(FishingSpotFlavor.LighthouseLane,
                new FishingSpotFlavorData(new Coordinates(187, 170 - 19),
                new Coordinates(187 + 1241, 170 - 19 + 577),
                new ScreenshotColor(38, 81, 135),
                new AbstractFishingRodAction.Tolerance(7, 15, 6)));

            elements.Add(FishingSpotFlavor.PunchlinePlaceHalloween,
                new FishingSpotFlavorData(new Coordinates(260, 196), new Coordinates(1349, 626),
                new ScreenshotColor(10, 76, 76), 8));
        }

        public static FishingSpotFlavorData GetDataFromItem(FishingSpotFlavor item)
        {
            return elements[item];
        }



        public Coordinates Scan1 { get; }
        public Coordinates Scan2 { get; }
        public ScreenshotColor BubbleColor { get; }
        public AbstractFishingRodAction.Tolerance Tolerance { get; }


        public FishingSpotFlavorData(Coordinates scan1, Coordinates scan2,
            ScreenshotColor bubbleColor, AbstractFishingRodAction.Tolerance tolerance)
        {
            this.Scan1 = scan1;
            this.Scan2 = scan2;
            this.BubbleColor = bubbleColor;
            this.Tolerance = tolerance;
        }


    }
}
