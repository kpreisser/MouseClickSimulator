using System;
using System.Text;

using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;

namespace TTMouseclickSimulator.Core.ToontownRewritten.Actions.Gardening;

/// <summary>
/// An action which plants a flower.
/// </summary>
public class PlantFlowerAction : AbstractAction
{
    private static readonly string[] jellybeanColors =
        { "Red", "Green", "Orange", "Violet", "Blue", "Pink", "Yellow", "Cyan", "Silver" };

    private const int MaxJellybeanCount = 8;

    private int[] jellybeanCombination;

    public PlantFlowerAction(int[] jellybeanCombination)
    {
        if (jellybeanCombination is null)
            throw new ArgumentNullException(nameof(jellybeanCombination));
        if (jellybeanCombination.Length < 1 || jellybeanCombination.Length > MaxJellybeanCount)
            throw new ArgumentException("A combination must consist of at least 1 and at most 8 jellybeans.");
        foreach (int jellybean in jellybeanCombination)
            if (jellybean < 0 || jellybean >= jellybeanColors.Length)
                throw new ArgumentException($"Invalid jellybean number: {jellybean}");

        this.jellybeanCombination = jellybeanCombination;
    }

    public override SimulatorCapabilities RequiredCapabilities
    {
        get => SimulatorCapabilities.MouseInput;
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        // Click on the "Plant Flower" button.
        MouseHelpers.DoSimpleMouseClick(
            provider,
            new Coordinates(76, 264),
            HorizontalScaleAlignment.Left);

        provider.Wait(200);

        // Click on the jellybean fields.
        foreach (int jellybean in this.jellybeanCombination)
        {
            var c = new Coordinates((int)Math.Round(560 + jellybean * 60.5), 514);
            MouseHelpers.DoSimpleMouseClick(
                provider,
                c,
                buttonDownDuration: 100);

            provider.Wait(100);
        }

        provider.Wait(100);

        // Click on the "Plant" button.
        MouseHelpers.DoSimpleMouseClick(provider, new Coordinates(975, 772));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < this.jellybeanCombination.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");

            sb.Append(jellybeanColors[this.jellybeanCombination[i]]);
        }

        return $"Plant Flower – JellyBeans: {sb}";
    }
}
