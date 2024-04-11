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

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Provides methods for invoking functionality on the Main (UI) thread.
    /// </summary>
    internal static class DispatcherExtensions
    {
#if MAUI
        internal static void Dispatch(this BindableObject bindable, Action action)
        {
            if (bindable.Dispatcher.IsDispatchRequired)
                bindable.Dispatcher.Dispatch(action);
            else
                action();
        }
#elif WPF
        internal static void Dispatch(this System.Windows.Threading.DispatcherObject dObject, Action action)
        {
            if (dObject.Dispatcher.CheckAccess())
                action();
            else
                dObject.Dispatcher.Invoke(action);
        }
#endif
    }
}

