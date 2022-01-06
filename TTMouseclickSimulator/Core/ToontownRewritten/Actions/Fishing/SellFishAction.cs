using System.Threading.Tasks;

using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class SellFishAction : AbstractAction
    {
        public SellFishAction()
        {
        }

        public override sealed async Task RunAsync(IInteractionProvider provider)
        {
            var c = new Coordinates(1159, 911);
            await MouseHelpers.DoSimpleMouseClickAsync(provider, c);
        }

        public override string ToString() => "Sell Fish";
    }
}
