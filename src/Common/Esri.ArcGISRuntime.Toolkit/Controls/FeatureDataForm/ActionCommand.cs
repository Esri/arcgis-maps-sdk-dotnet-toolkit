// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://opensource.org/licenses/ms-pl for details.
// All other rights reserved.

using System;
using System.Windows.Input;

/// <summary>
/// Very simple command that takes an <see cref="System.Action"/> with 
/// no parameters and invokes the  <see cref="System.Action"/> when 
/// the <see cref="ICommand.Execute(object)"/> method is invoked.
/// </summary>
internal class ActionCommand : ICommand
{
    #region Private Members

    // simple property to hold the command action.
    // this is the action that will be performed
    // when the execute method is invoked.
    private readonly Action _executeAction;
    private readonly Func<bool> _canExecuteFunction;

    #endregion Private Members

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionCommand" /> class.
    /// </summary>
    /// <param name="executeAction">The action to be performed on <see cref="ICommand.Execute(object)" />.</param>
    /// <param name="canExecuteFunction">The fuction that will test if the action can be executed.</param>
    public ActionCommand(Action executeAction, Func<bool> canExecuteFunction)
    {
        _executeAction = executeAction;
        _canExecuteFunction = canExecuteFunction;
    }

    #endregion Constructor

    #region ICommand Implementation

    /// <summary>
    /// Defines the method that determines whether 
    /// the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.  
    /// If the command does not require data to be passed, 
    /// this object can be set to null.</param>
    /// <returns>
    /// true if this command can be executed; 
    /// otherwise, false.
    /// </returns>
    public bool CanExecute(object parameter)
    {
        return (_executeAction != null && _canExecuteFunction != null && _canExecuteFunction());                    
    }

    /// <summary>
    /// Defines the method to be called when 
    /// the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command.  
    /// If the command does not require data to be passed, 
    /// this object can be set to null.</param>
    public void Execute(object parameter)
    {
        if (CanExecute(parameter))
            _executeAction();
    }

    /// <summary>
    /// Raises the can execute function
    /// </summary>
    public void RaiseCanExecute()
    {
        if(CanExecuteChanged != null)
            CanExecuteChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Occurs when changes occur that affect 
    /// whether or not the command should execute.
    /// </summary>
    public event EventHandler CanExecuteChanged; 

    #endregion ICommand Implementation        
}