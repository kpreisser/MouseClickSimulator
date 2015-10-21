using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Speedchat
{
    public class SpeedchatAction : IAction
    {

        private readonly int[] menuItems;
        private static readonly int[] xWidths = {
            215,
            215 + 250,
            215 + 250 + 180
        };

        public SpeedchatAction(params int[] menuItems)
        {
            this.menuItems = menuItems;
            if (menuItems.Length > 3)
                throw new ArgumentException("Only 3 levels are supported.");
            if (menuItems.Length == 0)
                throw new ArgumentException("The menuItems array must not be empty.");
        }

        public async Task RunAsync(IInteractionProvider provider)
        {
            // Click on the Speedchat Icon.
            Coordinates c = new Coordinates(122, 40);
            await MouseHelpers.DoSimpleMouseClickAsync(provider, c, 100);

            int currentYNumber = 0;
            for (int i = 0; i < menuItems.Length; i++)
            {
                await provider.WaitAsync(300);

                currentYNumber += menuItems[i];
                c = new Coordinates(xWidths[i], (40 + currentYNumber * 38));
                await MouseHelpers.DoSimpleMouseClickAsync(provider, c, 100);
            }
        }
    }
}
