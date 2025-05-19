using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace mbclientava.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _can;
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _can = canExecute;
        }
        public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
        public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
