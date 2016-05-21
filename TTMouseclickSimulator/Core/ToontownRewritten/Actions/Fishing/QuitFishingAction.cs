using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class QuitFishingAction : AbstractAction
    {
        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            Coordinates c = new Coordinates(1503, 1086);
            await MouseHelpers.DoSimpleMouseClickAsync(provider, c);
        }


        public override string ToString() => "Quit Fishing";
    }
}
