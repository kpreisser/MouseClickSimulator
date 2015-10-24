using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Environment
{

    /// <summary>
    /// Environment interface for Toontown Rewritten.
    /// </summary>
    public class TTRWindowsEnvironment : AbstractWindowsEnvironment
    {
        private const string ProcessName = "TTREngine";

        public static TTRWindowsEnvironment Instance { get; } = new TTRWindowsEnvironment();

        private TTRWindowsEnvironment()
        {

        }

        public override sealed Process FindProcess()
        {
            return FindProcessByName(ProcessName);
        }


        protected override sealed void ValidateWindowPosition(WindowPosition pos)
        {
            // Check if the aspect ratio of the window is 4:3 or higher.
            if (!(((double)pos.Size.Width / pos.Size.Height) >= 4d / 3d))
                throw new ArgumentException("The TT Rewritten window must have an aspect ratio " 
                    + "of 4:3 or higher (e.g. 16:9).");
        }
    }
}
