using System;
using System.Windows;

namespace TTMouseclickSimulator
{
    public partial class App : Application
    {
        static App()
        {
            // Enable automatic per-monitor DPI scaling. This can be removed once we
            // target .NET Framework 4.6.2 or higher.
            AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges", false);
        }
    }
}
