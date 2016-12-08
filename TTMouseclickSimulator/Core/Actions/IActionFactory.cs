namespace TTMouseclickSimulator.Core.Actions
{
    interface IActionFactory<out T> : IActionFactory where T : IAction
    {
        new T CreateAction();
    }

    interface IActionFactory
    {
        IAction CreateAction();
    }
}
