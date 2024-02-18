using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Fishing;

public class FishingSpotData
{
    public Coordinates Scan1 { get; }
    public Coordinates Scan2 { get; }
    public ScreenshotColor BubbleColor { get; }
    public AbstractFishingRodAction.Tolerance Tolerance { get; }

    public FishingSpotData(
        Coordinates scan1,
        Coordinates scan2,
        ScreenshotColor bubbleColor,
        AbstractFishingRodAction.Tolerance tolerance)
    {
        this.Scan1 = scan1;
        this.Scan2 = scan2;
        this.BubbleColor = bubbleColor;
        this.Tolerance = tolerance;
    }
}
