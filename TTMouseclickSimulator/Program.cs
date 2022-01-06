using System;
using System.Runtime.InteropServices;

namespace TTMouseclickSimulator
{
    internal static class Program
    {
        [STAThread]
        public static void Main()
        {
            // Try to set a better timer resolution than the default 15 ms.
            TrySetWindowsHighTimerResolution();

            // Enable support for long file paths.
            // Additionally, enable automatic per-monitor DPI scaling.
            // TODO: Remove these switches once we target.NET 4.6.2 or higher.
            AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling", false);
            AppContext.SetSwitch("Switch.System.IO.BlockLongPaths", false);
            AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges", false);

            App.Main();
        }

        /// <summary>
        /// When running on Windows, tries to request a timer resolution of 2 ms or better
        /// instead of the default resolution of 15 ms, which affects APIs like
        /// <see cref="Thread.Sleep(int)"/>.
        /// </summary>
        private static void TrySetWindowsHighTimerResolution()
        {
            try
            {
                // Request a higher timer resolution (2 ms) than the default
                // value of 15 ms. This is because starting with Windows 10 Version 2004,
                // each process will use this default timer resolution unless it calls
                // timeBeginPeriod to request a higher resolution.
                // This affects API like Thread.Sleep().
                TimeBeginPeriod(2);
            }
            catch
            {
                // Ignore.
            }
        }

        [DllImport("winmm.dll",
            EntryPoint = "timeBeginPeriod",
            ExactSpelling = true)]
        private static extern int TimeBeginPeriod(uint resolution);
    }
}
