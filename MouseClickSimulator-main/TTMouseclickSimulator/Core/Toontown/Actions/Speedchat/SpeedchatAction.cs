using System;
using System.Text;

using TTMouseClickSimulator.Core.Actions;
using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions.Speedchat;

public class SpeedchatAction : AbstractAction
{
    private static readonly int[] xWidths =
    {
        215,
        215 + 230,
        215 + 230 + 175,
        215 + 230 + 175 + 160
    };

    private readonly int[] menuItems;

    public SpeedchatAction(params int[] menuItems)
    {
        this.menuItems = menuItems;
        if (menuItems.Length > xWidths.Length)
            throw new ArgumentException($"Only {xWidths.Length} levels are supported.");
        if (menuItems.Length is 0)
            throw new ArgumentException("The menuItems array must not be empty.");
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        provider.ThrowIfNotToontownRewritten(nameof(SpeedchatAction));

        // Click on the Speedchat Icon.
        var c = new Coordinates(122, 40);
        MouseHelpers.DoSimpleMouseClick(provider, c, HorizontalScaleAlignment.Left, 100);

        int currentYNumber = 0;
        for (int i = 0; i < this.menuItems.Length; i++)
        {
            provider.Wait(300);

            currentYNumber += this.menuItems[i];

            c = new Coordinates(xWidths[i], 40 + currentYNumber * 37.55f);
            MouseHelpers.DoSimpleMouseClick(provider, c, HorizontalScaleAlignment.Left, 100);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < this.menuItems.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");

            sb.Append(this.menuItems[i]);
        }

        return $"Speedchat – Items: [{sb}]";
    }
}
