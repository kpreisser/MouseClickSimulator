using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTMouseclickSimulator.Core.Actions
{
    /// <summary>
    /// A generic factory for actions that have a zero args constructor.
    /// </summary>
    public class DefaultGenericActionFactory<T> : IActionFactory<T> where T : IAction, new()
    {
        public T CreateAction()
        {
            return new T();
        }

        IAction IActionFactory.CreateAction()
        {
            return CreateAction();
        }
    }
}
