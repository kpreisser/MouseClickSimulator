using System;
using System.Collections.Generic;

using TTMouseClickSimulator.Core.Environment;

namespace TTMouseClickSimulator.Core.Actions;

/// <summary>
/// Represents an action that runs another action in a loop.
/// </summary>
public class LoopAction : AbstractActionContainer
{
    private readonly IAction action;

    /// <summary>
    /// Specifies how often the action is run. null means infinite.
    /// </summary>
    private readonly int? count;

    public LoopAction(IAction action, int? count = null)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));
        if (count.HasValue && count.Value < 0)
            throw new ArgumentException("count must not be negative");

        this.action = action;
        this.count = count;
    }

    public override IReadOnlyList<IAction> SubActions
    {
        get => new IAction[] { this.action };
    }

    public override sealed void Run(IInteractionProvider provider)
    {
        this.OnSubActionStartedOrStopped(0);

        try
        {
            for (int i = 0; !this.count.HasValue || i < this.count.Value; i++)
            {
                while (true)
                {
                    try
                    {
                        provider.CancellationToken.ThrowIfCancellationRequested();

                        this.OnActionInformationUpdated($"Iteration {i + 1}/{this.count?.ToString() ?? "∞"}");
                        this.action.Run(provider);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        provider.CheckRetryForException(ex);
                        continue;
                    }

                    break;
                }
            }
        }
        finally
        {
            this.OnSubActionStartedOrStopped(null);
        }
    }

    public override string ToString()
    {
        return $"Loop – Count: {this.count?.ToString() ?? "∞"}";
    }
}
