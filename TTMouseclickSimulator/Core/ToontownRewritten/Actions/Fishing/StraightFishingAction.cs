using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class StraightFishingAction : AbstractFishingRodAction
    {
        protected override int WaitingForFishResultDialogTime => 25000;

        protected override sealed async Task FinishCastFishingRodAsync(IInteractionProvider provider)
        {
            // Simply cast the fishing rod straight, without checking for bubbles.
            Coordinates coords = new Coordinates(800, 1009);
            var pos = provider.GetCurrentWindowPosition();
            coords = pos.ScaleCoordinates(coords,
                MouseHelpers.ReferenceWindowSize);

            provider.MoveMouse(coords);
            await provider.WaitAsync(300);
            provider.ReleaseMouseButton();
        }


        public override string ToString() => "Straight Fishing Cast";
    }
}
