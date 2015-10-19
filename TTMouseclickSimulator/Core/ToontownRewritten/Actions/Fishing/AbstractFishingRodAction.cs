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
    public abstract class AbstractFishingRodAction : IAction
    {

        /// <summary>
        /// Coordinates to use when we check for a dialog that indicates that a fish
        /// has been catched.
        /// Those coordinates are adapted from the old tt mouse click simulator.
        /// </summary>
        private static readonly Coordinates[] fishResultDialogCoordinates =
        {
            new Coordinates(1023, 562),
            new Coordinates(634, 504),
            new Coordinates(564, 100)
        };
        private static readonly AbstractEnvironmentInterface.ScreenshotColor fishResultDialogColor =
            new AbstractEnvironmentInterface.ScreenshotColor(255, 255, 191);

        public async Task RunAsync(IInteractionProvider provider)
        {
            // Throw the fishing rod
            await StartThrowFishingRodAsync(provider);
            await FinishThrowFishingRodAsync(provider);


            // Then, wait until we find a window displaying the catched fish
            // or 33 seconds are over.
            Stopwatch sw = new Stopwatch();
            sw.Start();

            bool found = false;
            while (!found && sw.ElapsedMilliseconds <= 35000)
            {
                await provider.WaitAsync(1000);

                // Get a current screenshot.
                var windowPos = provider.GetCurrentWindowPosition();
                var screenshot = provider.CreateCurrentWindowScreenshot();

                foreach (Coordinates c in fishResultDialogCoordinates)
                {
                    var cc = windowPos.ScaleCoordinates(
                        c, MouseHelpers.ReferenceWindowSize);
                    var col = screenshot.GetPixel(cc);

                    if (CompareColor(fishResultDialogColor, col, 10))
                    {
                        // OK, we catched a fish, so break from the loop.
                        found = true;
                        break;
                    }
                }

            }
        }

        /// <summary>
        /// Clicks on the fishing rod button.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected async Task StartThrowFishingRodAsync(IInteractionProvider provider)
        {
            Coordinates coords = new Coordinates(800, 846);
            var pos = provider.GetCurrentWindowPosition();
            coords = pos.RelativeToAbsoluteCoordinates(pos.ScaleCoordinates(coords,
                MouseHelpers.ReferenceWindowSize));

            // Move the mouse and press the button.
            provider.MoveMouse(coords);
            provider.PressMouseButton();

            await provider.WaitAsync(300);
        }

        /// <summary>
        /// Detects a fish bubble and then throws the fishing rod by moving the mouse to the
        /// desired position and releaseing the mouse button.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected abstract Task FinishThrowFishingRodAsync(IInteractionProvider provider);


        protected bool CompareColor(AbstractEnvironmentInterface.ScreenshotColor refColor, 
            AbstractEnvironmentInterface.ScreenshotColor actualColor,
            int tolerance)
        {
            // Simply compare the discrepancy of the R, G and B values
            // of each color.
            for (int i = 0; i < 3; i++)
            {
                byte bRef = refColor.GetValueFromIndex(i);
                byte bAct = actualColor.GetValueFromIndex(i);

                if (!(Math.Abs(bRef - bAct) <= tolerance))
                    return false;
            }

            return true;
        }
    }
}
