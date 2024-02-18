using System;

using TTMouseClickSimulator.Core.Environment;
using TTMouseClickSimulator.Core.Toontown.Environment;

namespace TTMouseClickSimulator.Core.Toontown.Actions
{
    public static class InteractionProviderExtensions
    {
        public static void ThrowIfNotToontownRewritten(
            this IInteractionProvider provider,
            string actionName)
        {
            if (provider is not ToontownInteractionProvider ttProvider ||
                ttProvider.ToontownFlavor is not ToontownFlavor.ToontownRewritten)
            {
                throw new NotSupportedException(
                    $"{actionName} currently only supports Toontown Rewritten.");
            }
        }
    }
}
