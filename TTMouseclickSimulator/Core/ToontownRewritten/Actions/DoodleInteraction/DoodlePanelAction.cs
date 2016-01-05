using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.DoodleInteraction
{
    /// <summary>
    /// An action that clicks on the doodle interaction panel (Feed, Scratch, Call)
    /// </summary>
    public class DoodlePanelAction : AbstractAction
    {

        private readonly DoodlePanelButton button;


        public DoodlePanelAction(DoodlePanelButton button)
        {
            this.button = button;
        }

        public override async Task RunAsync(IInteractionProvider provider)
        {
            Coordinates c = new Coordinates(1397, 206 + (int)button * 49);
            await MouseHelpers.DoSimpleMouseClickAsync(provider, c, 200, VerticalScaleAlignment.Right);
        }


        public override string ToString() => $"Doodle Panel – Button: {button}";
        

        public enum DoodlePanelButton : int
        {
            Feed = 0,
            Scratch = 1,
            Call = 2
        }
    }
}
