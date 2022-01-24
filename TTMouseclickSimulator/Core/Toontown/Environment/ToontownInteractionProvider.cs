using System;
using System.Collections.Generic;
using System.Diagnostics;

using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Environment
{
    public class ToontownInteractionProvider : InteractionProvider
    {
        private const string TTRProcessNameX64 = "TTREngine64";

        private const string TTRProcessNameX86 = "TTREngine";

        private const string CCProcessName = "CorporateClash";

        public ToontownInteractionProvider(
            Simulator simulator,
            ToontownFlavor toontownFlavor,
            WindowsEnvironment environmentInterface,
            bool backgroundMode)
            : base(simulator, environmentInterface, backgroundMode)
        {
            this.ToontownFlavor = toontownFlavor;
        }

        public ToontownFlavor ToontownFlavor
        {
            get;
        }

        public bool UseWasdMovement
        {
            get;
            init;
        }

        protected override sealed List<Process> FindProcesses()
        {
            if (this.ToontownFlavor is ToontownFlavor.ToontownRewritten)
            {
                var processes = this.environmentInterface.FindProcessesByName(TTRProcessNameX64);
                processes.AddRange(this.environmentInterface.FindProcessesByName(TTRProcessNameX86));

                if (processes.Count is 0)
                {
                    throw new InvalidOperationException(
                        "Could not find Toontown Rewritten. Please make sure " +
                        "TT Rewritten is running before starting the simulator.\n\n" +
                        "If you're running Toontown Rewritten as administrator, you may also " +
                        "need to the simulator as administrator.");
                }

                return processes;
            }
            else if (this.ToontownFlavor is ToontownFlavor.CorporateClash)
            {
                var processes = this.environmentInterface.FindProcessesByName(CCProcessName);

                if (processes.Count is 0)
                {
                    throw new InvalidOperationException(
                        "Could not find Corporate Clash. Please make sure " +
                        "Corporate Clash is running before starting the simulator.\n\n" +
                        "If you're running Corporate Clash as administrator, you may also " +
                        "need to the simulator as administrator.");
                }

                return processes;
            }
            else
            {
                throw new NotSupportedException("Unsupported Toontown flavor: " + this.ToontownFlavor);
            }
        }

        protected override void TransformVirtualKey(ref WindowsEnvironment.VirtualKey key)
        {
            if (this.UseWasdMovement)
            {
                // Replace arrow keys with WASD keys.
                key = key switch
                {
                    WindowsEnvironment.VirtualKey.Up => WindowsEnvironment.VirtualKey.W,
                    WindowsEnvironment.VirtualKey.Down => WindowsEnvironment.VirtualKey.S,
                    WindowsEnvironment.VirtualKey.Left => WindowsEnvironment.VirtualKey.A,
                    WindowsEnvironment.VirtualKey.Right => WindowsEnvironment.VirtualKey.D,
                    var other => other
                };
            }
        }
    }
}
