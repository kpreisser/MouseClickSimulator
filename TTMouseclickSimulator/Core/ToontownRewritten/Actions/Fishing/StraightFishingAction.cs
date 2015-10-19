using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class StraightFishingAction : AbstractFishingRodAction
    {

        public StraightFishingAction()
            : base(20000)
        {

        }


        protected override async Task FinishThrowFishingRodAsync(IInteractionProvider provider)
        {
            // Simply throw the fishing rod straight, without checking for bubbles.
            Coordinates coords = new Coordinates(800, 1009);
            var pos = provider.GetCurrentWindowPosition();
            coords = pos.RelativeToAbsoluteCoordinates(pos.ScaleCoordinates(coords,
                MouseHelpers.ReferenceWindowSize));

            provider.MoveMouse(coords);
            await provider.WaitAsync(300);
            provider.ReleaseMouseButton();
        }
    }
}
