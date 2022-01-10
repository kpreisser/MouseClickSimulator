using System.Threading.Tasks;

using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions;

public static class MouseHelpers
{
    public static readonly Size ReferenceWindowSize = new(1600, 1151);

    public static async Task DoSimpleMouseClickAsync(
        IInteractionProvider provider,
        Coordinates coords,
        HorizontalScaleAlignment align = HorizontalScaleAlignment.Center,
        int buttonDownTimeout = 150)
    {
        var pos = provider.GetCurrentWindowPosition();
        coords = pos.ScaleCoordinates(
            coords,
            ReferenceWindowSize,
            align);

        provider.MoveMouse(coords);
        provider.PressMouseButton();
        await provider.WaitAsync(buttonDownTimeout);
        provider.ReleaseMouseButton();
    }

}
