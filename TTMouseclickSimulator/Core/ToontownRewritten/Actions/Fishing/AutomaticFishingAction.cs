using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class AutomaticFishingAction : AbstractFishingRodAction
    {
        private FishingSpotFlavor flavor;

        protected override int WaitingForFishResultDialogTime
        {
            get { return 6000; }
        }

        public AutomaticFishingAction(FishingSpotFlavor flavor)
        {
            this.flavor = flavor;
        }
        

        protected override sealed async Task FinishThrowFishingRodAsync(IInteractionProvider provider)
        {
            // Try to find a bubble.
            OnActionInformationUpdated("Scanning fish bubbles…");

            const int scanStep = 15;
            FishingSpotFlavorData spotData = FishingSpotFlavorData.GetDataFromItem(flavor);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IScreenshotContent screenshot;
            Coordinates? oldCoords = null;
            Coordinates? newCoords;
            int coordsMatchCounter = 0;
            while (true)
            {
                screenshot = provider.CreateCurrentWindowScreenshot();
                newCoords = null;

                // TODO: The fish bubble detection should be changed so that it does not scan
                // for a specific color, but instead checks that for a point if the color is
                // darker than the neighbor pixels (in some distance).
                for (int y = spotData.Scan1.Y; y <= spotData.Scan2.Y && !newCoords.HasValue; y += scanStep)
                {
                    for (int x = spotData.Scan1.X; x <= spotData.Scan2.X; x += scanStep)
                    {
                        var c = new Coordinates(x, y);
                        c = screenshot.WindowPosition.ScaleCoordinates(c,
                            MouseHelpers.ReferenceWindowSize);
                        if (CompareColor(spotData.BubbleColor, screenshot.GetPixel(c),
                            spotData.Tolerance))
                        {
                            int xc = x + 15;
                            int yc = y + 30;
                            newCoords = new Coordinates(xc, yc);
                            OnActionInformationUpdated($"Found bubble at {xc}, {yc}…");
                            break;
                        }
                    }
                }

                if (newCoords.HasValue && oldCoords.HasValue 
                    && Math.Abs(oldCoords.Value.X - newCoords.Value.X) <= scanStep
                    && Math.Abs(oldCoords.Value.Y - newCoords.Value.Y) <= scanStep)
                {
                    // The new coordinates are (nearly) the same as the previous ones.
                    coordsMatchCounter++;
                }
                else
                {
                    // Reset the counter and update the coordinates even if we currently didn't
                    // find them.
                    oldCoords = newCoords;
                    coordsMatchCounter = 0;
                }

                if (coordsMatchCounter == 2)
                {
                    // If we found the same coordinates two times, we assume
                    // the bubble is not moving at the moment.
                    break;
                }

                await provider.WaitAsync(500);

                // Ensure we don't wait longer than 36 seconds.
                if (sw.ElapsedMilliseconds >= 36000)
                    break;
            }

            if (!newCoords.HasValue && oldCoords.HasValue)
            {
                // Ensure we use the old coordinates if we have them but didn't
                // find new coordinates.
                newCoords = oldCoords;
            }

            if (!newCoords.HasValue)
            {
                // If we couldn't find the bubble we use default destination x,y values.
                OnActionInformationUpdated("No fish bubble found.");
                newCoords = new Coordinates(800, 1009);
            }
            else
            {
                // Calculate the destination coordinates.
                newCoords = new Coordinates(
                    (int)Math.Round(800d + 120d / 429d * (800d - newCoords.Value.X)),
                    (int)Math.Round(846d + 169d / 428d * (820d - newCoords.Value.Y))
                );
            }

            // Note: Instead of using a center position for scaling the X coordinate,
            // TTR seems to interpret it as being scaled from an 4/3 ratio. Therefore
            // we need to specify "NoAspectRatio" here.
            // However it could be that they will change this in the future, then 
            // we would need to use "Center".
            // Note: We assume the point to click on is exactly centered. Otherwise
            // we would need to adjust the X coordinate accordingly.
            var coords = screenshot.WindowPosition.RelativeToAbsoluteCoordinates(
                screenshot.WindowPosition.ScaleCoordinates(newCoords.Value,
                MouseHelpers.ReferenceWindowSize, VerticalScaleAlignment.NoAspectRatio));

            provider.MoveMouse(coords);
            await provider.WaitAsync(300);
            provider.ReleaseMouseButton();
        }
    }
}
