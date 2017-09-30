using TTMouseclickSimulator.Core;

namespace TTMouseclickSimulator.Project
{
    /// <summary>
    /// A simulator project that can be persisted.
    /// </summary>
    public class SimulatorProject
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public SimulatorConfiguration Configuration { get; set; }

        public override string ToString() => this.Title;
    }
}
