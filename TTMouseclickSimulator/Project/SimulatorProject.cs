using System;

using TTMouseClickSimulator.Core;

namespace TTMouseClickSimulator.Project;

/// <summary>
/// A simulator project that can be persisted.
/// </summary>
public class SimulatorProject
{
    public SimulatorProject(string title, string description, SimulatorConfiguration configuration)
    {
        this.Title = title ?? throw new ArgumentNullException(nameof(title));
        this.Description = description ?? throw new ArgumentNullException(nameof(description));
        this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string Title
    {
        get;
    }

    public string Description
    {
        get;
    }

    public SimulatorConfiguration Configuration
    {
        get;
    }

    public override string ToString()
    {
        return this.Title;
    }
}
