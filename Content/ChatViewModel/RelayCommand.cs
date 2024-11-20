// RelayCommand.cs
using System;
using System.Windows.Input;

namespace Content.ChatViewModel
{
    /// <summary>
    /// A generic implementation of the ICommand interface, allowing delegation of command logic.
    /// Used for binding commands to UI elements in MVVM architecture.
    /// </summary>

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class with the specified execute action and optional can-execute predicate.
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked.</param>
        /// <param name="canExecute">
        /// A predicate to determine whether the command can execute. If null, the command is always executable.
        /// </param>

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Evaluates whether the command can execute.
        /// </summary>
        /// <param name="parameter">The command parameter (not used in this implementation).</param>
        /// <returns>
        /// True if the command can execute; otherwise, false. Defaults to true if no predicate is provided.
        /// </returns>

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        /// <summary>
        /// Executes the command logic.
        /// </summary>
        /// <param name="parameter">The command parameter (not used in this implementation).</param>

        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// Event triggered when the execution state of the command changes.
        /// </summary>

        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Raises the CanExecuteChanged event to notify subscribers that the command's executable state may have changed.
        /// </summary>

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}