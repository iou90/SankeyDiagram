using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Kant.Wpf.Controls.Chart.Example
{
    public class CommandBase : ICommand
    {
        #region Constructor

        public CommandBase(Action command, Func<Object, Boolean> canExecute = null)
        {
            executeAction = command;
            executableFuncs = canExecute;
        }

        public CommandBase(Action<Object> paraCommand, Func<Object, Boolean> canExecute = null)
        {
            commandParameterAcions = paraCommand;
            executableFuncs = canExecute;
        }

        #endregion

        #region Methods

        public bool CanExecute(object parameter)
        {
            if (null != executableFuncs)
            {
                return executableFuncs(parameter);
            }
            else
            {
                return true;
            }
        }

        public void Execute(object parameter)
        {
            if (null != executeAction)
            {
                executeAction();
            }

            if (null != commandParameterAcions)
            {
                commandParameterAcions(parameter);
            }
        }

        #endregion

        #region Fields & Properties

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        private Action executeAction;

        private Action<Object> commandParameterAcions;

        private Func<Object, Boolean> executableFuncs;

        #endregion
    }
}
