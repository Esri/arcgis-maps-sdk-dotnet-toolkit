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

#if !__IOS__ && !__ANDROID__
using System;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// An implementation of an <see cref="ICommand"/>.
    /// </summary>
    internal class DelegateCommand : ICommand
    {
        private readonly Action<object?>? _onExecute;

        internal DelegateCommand(Action<object?> onExecute)
        {
            _onExecute = onExecute ?? throw new ArgumentNullException(nameof(onExecute));
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _onExecute?.Invoke(parameter);
    }
}
#endif