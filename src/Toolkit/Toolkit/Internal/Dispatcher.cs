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

#if XAMARIN
using System;
using System.Threading.Tasks;

#if __ANDROID__
using Android.OS;
#elif __IOS__
using CoreFoundation;
using Foundation;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Provides methods for invoking functionality on the Main (UI) thread.
    /// </summary>
    internal class Dispatcher
    {
#if __ANDROID__
        // Create a static instance of this and re-use so that only one peer Java instance is created.
        private static Handler _handler = new Handler(Looper.MainLooper);
#endif

        /// <summary>
        /// Executes the specified action on the UI thread asynchronously.
        /// </summary>
        /// <remarks>
        /// - Uses Xamarin.Forms Device class to invoke action on the UI thread
        /// - void return - fire and forget.
        /// </remarks>
        internal static void RunAsyncAction(Action a, bool highPriority = true)
        {
#if __IOS__
            DispatchQueue.MainQueue.DispatchAsync(a);
#elif __ANDROID__
            _handler.Post(a);
#endif
        }
    }
}

#endif
