using System;
using System.Collections.Generic;
using System.Diagnostics;

using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Environment;

/// <summary>
/// Environment interface for Toontown Rewritten on Windows.
/// </summary>
public class TTRWindowsEnvironment : AbstractWindowsEnvironment
{
    private const string ProcessName64 = "TTREngine64";

    private const string ProcessName32 = "TTREngine";

    private TTRWindowsEnvironment()
    {
    }

    public static TTRWindowsEnvironment Instance
    {
        get;
    } = new TTRWindowsEnvironment();

    public override sealed List<Process> FindProcesses()
    {
        var processes = FindProcessesByName(ProcessName64);
        processes.AddRange(FindProcessesByName(ProcessName32));

        if (processes.Count is 0)
        {
            throw new ArgumentException(
                "Could not find Toontown Rewritten. Please make sure " +
                "TT Rewritten is running before starting the simulator.\n\n" +
                "If you're running Toontown Rewritten as administrator, you may also " +
                "need to the simulator as administrator.");
        }

        return processes;
    }
}
