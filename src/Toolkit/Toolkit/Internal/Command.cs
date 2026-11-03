// /*******************************************************************************
//  * Copyright 2026 Esri
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

#if MAUI
using Microsoft.Maui.Controls;
#else

namespace Esri.ArcGISRuntime.Toolkit.Internal;

internal class Command : System.Windows.Input.ICommand
{
    private Action _execute;
    private Func<bool> _canExecute;

    public Command(Action execute, Func<bool> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    internal void ChangeCanExecute() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute();

    public void Execute(object? parameter) => _execute();
}
#endif
