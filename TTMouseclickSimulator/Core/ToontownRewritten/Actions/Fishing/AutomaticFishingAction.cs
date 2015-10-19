using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing
{
    public class AutomaticFishingAction : AbstractFishingRodAction
    {
        private FishingSpotFlavor flavor;

        public AutomaticFishingAction(FishingSpotFlavor flavor)
            : base(5000)
        {
            this.flavor = flavor;
        }

        
        protected override async Task FinishThrowFishingRodAsync(IInteractionProvider provider)
        {
            // TODO
        }
    }
}
