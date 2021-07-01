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

using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Preview.UI.Controls;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Preview.Authentication
{
    /// <summary>
    /// Custom <see cref="AuthenticationManager"/> Challenge Handler that combines user/password, certificate and OAuth authentication.
    /// </summary>
    public sealed class ChallengeHandler : Esri.ArcGISRuntime.Security.IChallengeHandler
    {
        private System.Windows.Threading.Dispatcher _dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChallengeHandler"/> class.
        /// </summary>
        /// <param name="dispatcher">The Dispatcher for the application.</param>
        public ChallengeHandler(System.Windows.Threading.Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new System.ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Gets or sets a value indicating whether credentials should be stored in the system's Credentials Cache.
        /// </summary>
        public bool EnableCredentialsCache { get; set; } = true;

        /// <summary>
        /// Gets or sets the style applied to the SignInForm.
        /// </summary>
        public Style? SignInFormStyle { get; set; }

        /// <inheritdoc />
        Task<Credential> IChallengeHandler.CreateCredentialAsync(CredentialRequestInfo info)
        {
            TaskCompletionSource<Credential> tcs = new TaskCompletionSource<Credential>();
            _dispatcher.InvokeAsync(() =>
            {
                if (info.AuthenticationType == AuthenticationType.Certificate)
                {
                    var cert = Authentication.CertificateHelper.SelectCertificate(info);
                    if (cert is null)
                    {
                        tcs.SetException(new System.InvalidOperationException("No certificate was selected."));
                    }
                    else
                    {
                        tcs.SetResult(cert);
                    }
                }
                else
                {
                    var login = CreateWindow(info);
                    login.Closed += (s, e) =>
                    {
                        var credential = (login.Content as SignInForm)?.Credential;
                        if (credential is null)
                        {
                            tcs.SetException(new System.InvalidOperationException(" No credential was provided."));
                        }
                        else
                        {
                            tcs.SetResult(credential);
                        }
                    };
                    login.ShowDialog();
                }
            });
            return tcs.Task;
        }

        private System.Windows.Window CreateWindow(CredentialRequestInfo info)
        {
            Window window = new Window()
            {
                Height = 470,
                Width = 681,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
            };
            var contentDialog = new SignInForm()
            {
                EnableCredentialCache = EnableCredentialsCache,
                CredentialRequestInfo = info,
            };

            if (SignInFormStyle != null)
            {
                contentDialog.Style = SignInFormStyle;
            }

            contentDialog.Completed += (s, e) =>
            {
                window.Close();
            };

            contentDialog.Cancelled += (s, e) =>
            {
                window.Close();
            };
            contentDialog.CreateCredentialError += (s, e) =>
            {
                MessageBox.Show("Sign in failed. Check your username and password", e.Message);
            };
            if (info.AuthenticationType == AuthenticationType.NetworkCredential)
            {
                contentDialog.MessageText = "Network Credentials required for ";
            }
            else if (info.AuthenticationType == AuthenticationType.Token)
            {
                contentDialog.MessageText = "ArcGIS Credentials required for ";
            }

            window.Content = contentDialog;
            window.Title = contentDialog.HeaderText;
            return window;
        }
    }
}
