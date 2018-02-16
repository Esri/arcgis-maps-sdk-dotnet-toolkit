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
    /// Provides methods for invoking functionality on the Main (UI) thread
    /// </summary>
    internal class Dispatcher
    {
        // Exclude Dispatcher.CheckAccess from Android (for now) so we don't use it without considering its
        // implications.  When this is called, native Java Looper instances are created that map to the managed
        // Looper representations.  Normally this is fine, but when called in rapid succession, the peer Java
        // instances can apparently get out of sync with their managed counterparts, and this seems further
        // exacerbated if there is asynchrony involved.  The end result is a broken broken CLR <--> JNI object
        // linkage, leading to a deleted global JNI ref error and crash.  See:
        // https://devtopia.esri.com/runtime/dotnet-api/issues/3765
        // https://devtopia.esri.com/runtime/dotnet-api/issues/4516
#if !__ANDROID__
        /// <summary>
        /// Check whether execution is currently running on the UI thread
        /// </summary>
        /// <returns>true if currently executing on UI thread, false otherwise</returns>
        internal static bool CheckAccess()
        {
            var hasAccess = true;

#if __ANDROID__
            using (Looper looper = Looper.MyLooper())
            {
                using (Looper mainLooper = Looper.MainLooper)
                {
                    hasAccess = looper == mainLooper;
                }
            }
#elif __IOS__
            hasAccess = NSThread.Current.IsMainThread;
#endif

            return hasAccess;
        }
#endif

#if __ANDROID__
        // Create a static instance of this and re-use so that only one peer Java instance is created.
        private static Handler _handler = new Handler(Looper.MainLooper);
#endif

        /// <summary>
        /// Executes the specified action on the UI thread asynchronously
        /// </summary>
        /// <remarks>
        /// - Uses Xamarin.Forms Device class to invoke action on the UI thread
        /// - void return - fire and forget
        /// </remarks>
        internal static void RunAsyncAction(Action a, bool highPriority = true)
        {
#if __IOS__
            DispatchQueue.MainQueue.DispatchAsync(a);
#elif __ANDROID__
            _handler.Post(a);
#endif
        }

        /// <summary>
        /// Executes the specified action on the UI thread asynchronously
        /// </summary>
        /// <returns>Task</returns>
        /// <remarks>
        /// - Uses Xamarin.Forms Device class to invoke action on the UI thread
        /// - Returns a task that can be awaited if necessary
        /// </remarks>
        internal static Task InvokeAsync(Action a)
        {
            var tcs = new TaskCompletionSource<bool>();
            Action action = () =>
            {
                try
                {
                    a();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };
            RunAsyncAction(action);
            return tcs.Task;
        }
    }
}

#endif
