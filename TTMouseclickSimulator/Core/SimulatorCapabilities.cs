using System;

namespace TTMouseclickSimulator.Core
{
    [Flags]
    public enum SimulatorCapabilities
    {
        None = 0,

        KeyboardInput = 1 << 0,

        MouseInput = 1 << 1,

        CaptureScreenshot = 1 << 2
    }
}
