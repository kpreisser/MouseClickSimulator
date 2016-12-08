namespace TTMouseclickSimulator.Core.Actions
{
    /// <summary>
    /// A generic factory for actions that have a zero args constructor.
    /// </summary>
    public class DefaultGenericActionFactory<T> : IActionFactory<T> where T : IAction, new()
    {
        public T CreateAction() => new T();
        
        IAction IActionFactory.CreateAction() => CreateAction();
    }
}
