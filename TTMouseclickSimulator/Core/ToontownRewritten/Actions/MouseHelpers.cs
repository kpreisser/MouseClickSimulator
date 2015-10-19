using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions
{
    public class MouseHelpers
    {
        public static readonly Size ReferenceWindowSize = new Size(1600, 1151);

        public static async Task DoSimpleMouseClickAsync(IInteractionProvider provider,
            Coordinates coords, int buttonDownTimeout)
        {
            var pos = provider.GetCurrentWindowPosition();
            coords = pos.RelativeToAbsoluteCoordinates(pos.ScaleCoordinates(coords,
                ReferenceWindowSize));

            provider.MoveMouse(coords);
            provider.PressMouseButton();
            await provider.WaitAsync(buttonDownTimeout);
            provider.ReleaseMouseButton();
        }

    }
}
