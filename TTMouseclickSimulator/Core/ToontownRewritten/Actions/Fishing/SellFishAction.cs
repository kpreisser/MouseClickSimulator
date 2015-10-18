using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class SellFishAction : IAction
    {
        private static readonly Size referenceSize = new Size(1600, 1151);

        public async Task RunAsync(IInteractionProvider provider)
        {
            Coordinates c = new Coordinates(1159, 911);
            var pos = provider.GetCurrentWindowPosition();
            c = pos.RelativeToAbsoluteCoordinates(pos.ConvertCoordinates(c, referenceSize));

            provider.MoveMouse(c);
            provider.PressMouseButton();
            await provider.WaitAsync(100);
            provider.ReleaseMouseButton();
        }
    }
}
