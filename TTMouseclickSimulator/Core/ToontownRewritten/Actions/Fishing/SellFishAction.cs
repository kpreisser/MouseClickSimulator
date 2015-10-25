using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class SellFishAction : AbstractAction
    {
        
        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            Coordinates c = new Coordinates(1159, 911);
            await MouseHelpers.DoSimpleMouseClickAsync(provider, c, 200);
        }


        public override string ToString()
        {
            return "Sell Fish";
        }

    }
}
