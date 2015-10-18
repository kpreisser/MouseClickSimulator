using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class QuitFishingAction : IAction
    {

        public async Task RunAsync(IInteractionProvider provider)
        {
            Coordinates c = new Coordinates(1503, 1086);
            var pos = provider.GetCurrentWindowPosition();
            c = pos.RelativeToAbsoluteCoordinates(pos.ConvertCoordinates(c,
                Constants.ReferenceWindowSize));

            provider.MoveMouse(c);
            provider.PressMouseButton();
            await provider.WaitAsync(100);
            provider.ReleaseMouseButton();
        }

    }
}
