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

        protected override int WaitingForFishResultDialogTime
        {
            get { return 20000; }
        }

        protected override sealed async Task FinishThrowFishingRodAsync(IInteractionProvider provider)
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


        public override string ToString()
        {
            return "Straight Fishing Cast";
        }
    }
}
