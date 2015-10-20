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

        public AutomaticFishingAction(FishingSpotFlavor flavor)
            : base(5000)
        {
            this.flavor = flavor;
        }

        
        protected override async Task FinishThrowFishingRodAsync(IInteractionProvider provider)
        {
            // Try to find a bubble.
            const int scanStep = 15;

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
                for (int y = flavor.Scan1.Y; y <= flavor.Scan2.Y && !newCoords.HasValue; y += scanStep)
                {
                    for (int x = flavor.Scan1.X; x <= flavor.Scan2.X; x += scanStep)
                    {
                        var c = new Coordinates(x, y);
                        c = screenshot.WindowPosition.ScaleCoordinates(c,
                            MouseHelpers.ReferenceWindowSize);
                        if (CompareColor(flavor.BubbleColor, screenshot.GetPixel(c),
                            flavor.Tolerance))
                        {
                            newCoords = new Coordinates(x, y + 15);
                            break;
                        }
                    }
                }

                if (newCoords.HasValue && oldCoords.HasValue 
                    && Math.Abs(oldCoords.Value.X - newCoords.Value.X) <= scanStep
                    && Math.Abs(oldCoords.Value.Y - newCoords.Value.Y) <= scanStep)
                {
                    // The new coordinates are (nearly) the same as the previous ones.
                    //oldCoords = newCoords;
                    coordsMatchCounter++;
                }
                else
                {
                    // Only update the coords and reset the counter if the
                    // new ones have a value.
                    if (newCoords.HasValue)
                    {
                        oldCoords = newCoords;
                        coordsMatchCounter = 0;
                    }
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
                // TODO
                newCoords = new Coordinates(800, 1009);
            }
            else
            {
                // Calculate the destination coordinates.
                newCoords = new Coordinates(
                    (int)Math.Round(800d + 120d * (800d - newCoords.Value.X) / 429d),
                    (int)Math.Round(846d + 169d * (820d - newCoords.Value.Y) / 428d)
                );
            }


            var coords = screenshot.WindowPosition.RelativeToAbsoluteCoordinates(
                screenshot.WindowPosition.ScaleCoordinates(newCoords.Value,
                MouseHelpers.ReferenceWindowSize));

            provider.MoveMouse(coords);
            await provider.WaitAsync(300);
            provider.ReleaseMouseButton();
        }
    }
}
