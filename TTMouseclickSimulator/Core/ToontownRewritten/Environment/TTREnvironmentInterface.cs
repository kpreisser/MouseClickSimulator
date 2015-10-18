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
    public class TTREnvironmentInterface : AbstractEnvironmentInterface
    {
        private const string ProcessName = "TTREngine";

        public override Process FindProcess()
        {
            return FindProcessByName(ProcessName);
        }
    }
}
