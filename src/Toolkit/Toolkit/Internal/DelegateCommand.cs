// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Simple command implementation.
    /// </summary>
#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    internal partial class DelegateCommand : ICommand
    {
        private bool _canExecute = true;
        private readonly Action<object?>? _onExecute;
        private readonly Func<object?, bool>? _canExecuteFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        public DelegateCommand(Action inputAction) => _onExecute = (o) => inputAction();

        internal DelegateCommand(Action<object?> onExecute) : this(onExecute, (Func<object?, bool>?)null)
        {
        }
        
        internal DelegateCommand(Action<object?> onExecute, Func<bool>? canExecute) :this (onExecute, canExecute is null ? null : (o) => canExecute.Invoke())
        {
        }

        internal DelegateCommand(Action<object?> onExecute, Func<object?, bool>? canExecute)
        {
            _onExecute = onExecute ?? throw new ArgumentNullException(nameof(onExecute));
            _canExecuteFunc = canExecute;
        }

        /// <inheritdoc/>
        public event EventHandler? CanExecuteChanged;

        /// <inheritdoc/>
        public bool CanExecute(object? parameter)
        {
            return _canExecuteFunc?.Invoke(parameter) ?? _canExecute;
        }

        /// <inheritdoc/>
        public void Execute(object? parameter)
        {
            _onExecute?.Invoke(parameter);
        }

        internal void NotifyCanExecuteChanged()
        {
            System.Diagnostics.Debug.Assert(_canExecuteFunc != null);
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        internal void NotifyCanExecuteChanged(bool newValue)
        {
            System.Diagnostics.Debug.Assert(_canExecuteFunc == null);
            if (newValue != _canExecute)
            {
                _canExecute = newValue;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
