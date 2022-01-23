namespace TTMouseClickSimulator.Core
{
    public static class SimulatorCapabilitiesExtensions
    {
        public static bool IsSet(
            this SimulatorCapabilities capabilities,
            SimulatorCapabilities value)
        {
            return (capabilities & value) == value;
        }
    }
}
