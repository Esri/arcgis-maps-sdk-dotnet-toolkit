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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.UI;

namespace Esri.ArcGISRuntime.Toolkit.Authentication
{
    /// <summary>
    /// A default challenge handler capable of responding to authentication requests from ArcGIS services.
    /// </summary>
    public class ChallengeHandler : ChallengeHandlerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChallengeHandler"/> class.
        /// </summary>
        public ChallengeHandler()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChallengeHandler"/> class.
        /// </summary>
        /// <param name="mainWindow">The main window of the application.</param>
        public ChallengeHandler(Window? mainWindow)
        {
            MainWindow = mainWindow;
        }

        /// <inheritdoc/>
        protected override Task<System.Net.NetworkCredential> RequestUsernamePasswordAsync(CredentialRequestInfo info)
        {
            var tcs = new TaskCompletionSource<System.Net.NetworkCredential>();
            DispatchIfNeeded(() =>
            {
                var dialog = new CredentialDialog(info);
                if (dialog.ShowDialog(MainWindow))
                {
                    tcs.TrySetResult(new System.Net.NetworkCredential(dialog.UserName, dialog.Password));
                }
                else
                {
                    tcs.TrySetException(new InvalidOperationException("No credential was specified."));
                }
            });
            return tcs.Task;
        }

        /// <inheritdoc/>
        protected override Task<X509Certificate2> RequestCertificateAsync(CredentialRequestInfo info)
        {
            var tcs = new TaskCompletionSource<X509Certificate2>();
            DispatchIfNeeded(() =>
            {
                var certificate = CertificateHelper.SelectCertificate(info, MainWindow);
                if (certificate != null)
                {
                    tcs.TrySetResult(certificate);
                }
                else
                {
                    tcs.TrySetException(new InvalidOperationException("No certificate was selected."));
                }
            });
            return tcs.Task;
        }

        // Executes the specified callback using the application dispatcher if needed.
        private void DispatchIfNeeded(Action callback)
        {
            var dispatcher = MainWindow?.Dispatcher ?? Application.Current.Dispatcher;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(callback);
            }
            else
            {
                callback();
            }
        }

        /// <summary>
        /// Gets or sets the main window of the application.
        /// </summary>
        /// <remarks>
        /// If no window is specified, the challenge handler will attempt to use the current active window.
        /// </remarks>
        public Window? MainWindow { get; set; }
    }
}
