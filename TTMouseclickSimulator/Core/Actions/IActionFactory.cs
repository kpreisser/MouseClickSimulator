using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
