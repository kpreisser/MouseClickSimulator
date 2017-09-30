using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions
{
    public class MouseHelpers
    {
        public static readonly Size ReferenceWindowSize = new Size(1600, 1151);

        public static async Task DoSimpleMouseClickAsync(IInteractionProvider provider,
            Coordinates coords, VerticalScaleAlignment valign = VerticalScaleAlignment.Center,
            int buttonDownTimeout = 150)
        {
            var pos = provider.GetCurrentWindowPosition();
            coords = pos.ScaleCoordinates(coords,
                ReferenceWindowSize, valign);

            provider.MoveMouse(coords);
            provider.PressMouseButton();
            await provider.WaitAsync(buttonDownTimeout);
            provider.ReleaseMouseButton();
        }

    }
}
