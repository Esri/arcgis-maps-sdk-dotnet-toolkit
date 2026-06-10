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

#nullable enable

using Esri.ArcGISRuntime.Tasks;
using System.Diagnostics;
#if __IOS__
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BackgroundTasks;
using Foundation;
using ObjCRuntime;
#endif

namespace Esri.ArcGISRuntime.Toolkit
{
    public sealed partial class OfflineManager
    {
#if __IOS__
        [SupportedOSPlatform("ios26.0")]
        private void StartContinuedProcessingTask(IJob job, string subtitle)
        {
            var bundleIdentifier = NSBundle.MainBundle.BundleIdentifier;
            if (string.IsNullOrWhiteSpace(bundleIdentifier))
            {
                Trace.WriteLine("Unable to start a continued processing task because the app bundle identifier is missing.", "ArcGIS Toolkit");
                return;
            }

            var identifier = $"{bundleIdentifier}.cpt.jobs.{Guid.NewGuid():D}";
            BGTaskScheduler.Shared.Register(identifier, null, task =>
            {
                if (GetContinuedProcessingProgress(task) is null)
                {
                    task.SetTaskCompleted(false);
                    return;
                }

                BindToContinuedProcessingTask(task, job);
            });

            using var request = CreateContinuedProcessingTaskRequest(identifier, subtitle);
            if (request is null)
            {
                return;
            }

            BGTaskScheduler.Shared.Submit(request, out var error);
            if (error is not null)
            {
                Trace.WriteLine($"Error scheduling continued processing task: {error.LocalizedDescription}", "ArcGIS Toolkit");
            }
        }

        [SupportedOSPlatform("ios26.0")]
        private BGTaskRequest? CreateContinuedProcessingTaskRequest(string identifier, string subtitle)
        {
            var requestClassHandle = Class.GetHandle("BGContinuedProcessingTaskRequest");
            if (requestClassHandle == IntPtr.Zero)
            {
                Trace.WriteLine("BGContinuedProcessingTaskRequest is not available in the current runtime.", "ArcGIS Toolkit");
                return null;
            }

            using var requestIdentifier = new NSString(identifier);
            using var requestTitle = new NSString(Properties.Resources.GetString("OfflineMapAreasBackgroundTaskTitle"));
            using var requestSubtitle = new NSString(subtitle);

            var allocatedHandle = IntPtr_objc_msgSend(requestClassHandle, Selector.GetHandle("alloc"));
            var requestHandle = IntPtr_objc_msgSend_IntPtr_IntPtr_IntPtr(
                allocatedHandle,
                Selector.GetHandle("initWithIdentifier:title:subtitle:"),
                requestIdentifier.Handle,
                requestTitle.Handle,
                requestSubtitle.Handle);

            return Runtime.GetNSObject<BGTaskRequest>(requestHandle, owns: true);
        }

        [SupportedOSPlatform("ios26.0")]
        private void BindToContinuedProcessingTask(BGTask task, IJob job)
        {
            task.ExpirationHandler = () => _ = job.CancelAsync();

            var taskProgress = GetContinuedProcessingProgress(task);
            if (taskProgress is null)
            {
                task.SetTaskCompleted(false);
                return;
            }

            taskProgress.TotalUnitCount = 100;
            taskProgress.CompletedUnitCount = job.Progress;

            EventHandler<EventArgs> progressChanged = (_, _) => taskProgress.CompletedUnitCount = job.Progress;
            job.ProgressChanged += progressChanged;

            _ = Task.Run(async () =>
            {
                try
                {
                    await job.GetResultAsync().ConfigureAwait(false);
                    task.SetTaskCompleted(true);
                }
                catch
                {
                    task.SetTaskCompleted(false);
                }
                finally
                {
                    job.ProgressChanged -= progressChanged;
                }
            });
        }

        [SupportedOSPlatform("ios26.0")]
        private static NSProgress? GetContinuedProcessingProgress(BGTask task)
        {
            var progressHandle = IntPtr_objc_msgSend(task.Handle, Selector.GetHandle("progress"));
            return progressHandle == IntPtr.Zero ? null : Runtime.GetNSObject<NSProgress>(progressHandle, owns: false);
        }

        private const string ObjectiveCLibrary = "/usr/lib/libobjc.dylib";

        [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        private static extern IntPtr IntPtr_objc_msgSend_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);
#endif
    }
}
