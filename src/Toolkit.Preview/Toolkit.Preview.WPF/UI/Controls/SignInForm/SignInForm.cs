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
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Preview.Authentication;

namespace Esri.ArcGISRuntime.Toolkit.Preview.UI.Controls
{
    /// <summary>
    /// A Sign-in form for generating either an ArcGIS Token Credential or a Network Credential.
    /// </summary>
    public class SignInForm : Control
    {
        private ServerInfo? _serverInfo;
        private ToggleButton? _rememberCredentialsButton;
        private PasswordBox? _password;
        private TextBox? _username;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignInForm"/> class.
        /// </summary>
        public SignInForm()
        {
            DefaultStyleKey = typeof(SignInForm);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _rememberCredentialsButton = GetTemplateChild("RememberCredentials") as ToggleButton;
            if (_rememberCredentialsButton != null)
            {
                _rememberCredentialsButton.Visibility = EnableCredentialCache ? Visibility.Visible : Visibility.Collapsed;
            }

            _username = GetTemplateChild("Username") as TextBox;
            _password = GetTemplateChild("Password") as PasswordBox;

            if (GetTemplateChild("OkButton") is ButtonBase okButton)
            {
                okButton.Click += OkButton_Click;
            }

            if (GetTemplateChild("CancelButton") is ButtonBase cancelButton)
            {
                cancelButton.Click += CancelButton_Click;
            }

            PopulateFields();
        }

        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            var info = CredentialRequestInfo;
            if (info == null || _username == null || _password == null)
            {
                return;
            }

            if (info.AuthenticationType == AuthenticationType.Token)
            {
                if (info.ServiceUri == null)
                {
                    return;
                }

                try
                {
                    Credential = await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, _username.Text, ConvertToUnsecureString(_password.SecurePassword), info.GenerateTokenOptions);
                }
                catch (System.Exception ex)
                {
                    CreateCredentialError?.Invoke(this, ex);
                    return;
                }
            }
            else if (info.AuthenticationType == AuthenticationType.NetworkCredential)
            {
                Credential = new ArcGISNetworkCredential() { Credentials = new System.Net.NetworkCredential(_username.Text, _password.SecurePassword) };
            }
            else
            {
                CreateCredentialError?.Invoke(this, new NotSupportedException("Authentication type not supported"));
            }

            if (EnableCredentialCache)
            {
                var host = (_serverInfo == null ? info.ServiceUri : _serverInfo.ServerUri) !;
                if (_rememberCredentialsButton != null && _rememberCredentialsButton.IsChecked.HasValue && _rememberCredentialsButton.IsChecked.Value)
                {
                    CredentialsCache.SaveCredential(_username.Text, _password.SecurePassword, host);
                }
                else
                {
                    CredentialsCache.DeleteCredential(host);
                }
            }

            if (Credential != null)
            {
                Completed?.Invoke(this, Credential);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateFields()
        {
            if (_rememberCredentialsButton != null)
            {
                _rememberCredentialsButton.IsChecked = false;
            }

            if (_username != null)
            {
                _username.Text = string.Empty;
            }

            if (_password != null)
            {
                _password.Password = string.Empty;
            }

            if (ServerHost != null && EnableCredentialCache)
            {
                var credential = CredentialsCache.ReadCredential(ServerHost);
                if (credential != null)
                {
                    if (_username != null)
                    {
                        _username.Text = credential.Username;
                    }

                    if (_password != null)
                    {
                        _password.Password = ConvertToUnsecureString(credential.Password);
                    }

                    if (_rememberCredentialsButton != null)
                    {
                        _rememberCredentialsButton.IsChecked = true;
                    }
                }
            }
        }

        /// <summary>
        /// Raised when the cancel button is pressed
        /// </summary>
        public event EventHandler? Cancelled;

        /// <summary>
        /// Raised when a credential has successfully been created
        /// </summary>
        public event EventHandler<Credential>? Completed;

        /// <summary>
        /// Raised it the form failed to generate a credential from the supplied username and password
        /// </summary>
        public event EventHandler<Exception>? CreateCredentialError;

        /// <summary>
        /// Gets the credential generated by the dialog.
        /// </summary>
        public Credential? Credential { get; private set; }

        private static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
            {
                return string.Empty;
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString) ?? string.Empty;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        /// Gets the URI of the server a credential is currently being created for.
        /// This value is extracted from the value of the <see cref="CredentialRequestInfo"/>
        /// and the <see cref="AuthenticationManager"/>.
        /// </summary>
        public Uri? ServerHost
        {
            get { return GetValue(ServerHostPropertyKey.DependencyProperty) as Uri; }
        }

        private static readonly DependencyPropertyKey ServerHostPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ServerHost), typeof(Uri), typeof(SignInForm), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ServerHost"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ServerHostProperty = ServerHostPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets or sets the message displayed to the user.
        /// </summary>
        public string? MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MessageText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register(nameof(MessageText), typeof(string), typeof(SignInForm), new PropertyMetadata("Please enter credentials for"));

        /// <summary>
        /// Gets or sets the credential info used to generated a credential from.
        /// </summary>
        public CredentialRequestInfo? CredentialRequestInfo
        {
            get { return (CredentialRequestInfo)GetValue(CredentialRequestInfoProperty); }
            set { SetValue(CredentialRequestInfoProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OkButtonText"/> dependency property.
        /// </summary>
        private static readonly DependencyProperty CredentialRequestInfoProperty =
            DependencyProperty.Register(nameof(CredentialRequestInfo), typeof(CredentialRequestInfo), typeof(SignInForm), new PropertyMetadata(null, OnCredentialRequestInfoPropertyChanged));

        private static void OnCredentialRequestInfoPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SignInForm)d;
            ctrl.InitializeCredentials();
        }

        private void InitializeCredentials()
        {
            var info = CredentialRequestInfo;
            if (info != null && info.AuthenticationType == AuthenticationType.Certificate)
            {
                // Use Authentication.CertificateHelper.SelectCertificate instead
                throw new NotSupportedException("Certificate Authentication not supported by this control");
            }

            Uri? serverHost = null;
            _serverInfo = null;
            if (info != null && info.ServiceUri != null)
            {
                _serverInfo = AuthenticationManager.Current.FindServerInfo(info.ServiceUri);
                serverHost = _serverInfo == null ? info.ServiceUri : _serverInfo.ServerUri;
            }

            SetValue(ServerHostPropertyKey, serverHost);
            PopulateFields();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user is presented with an option to
        /// cache the entered credential to the <see cref="Authentication.CredentialsCache"/>.
        /// </summary>
        /// <seealso cref="Authentication.CredentialsCache"/>
        public bool EnableCredentialCache
        {
            get { return (bool)GetValue(EnableCredentialCacheProperty); }
            set { SetValue(EnableCredentialCacheProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="EnableCredentialCache"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableCredentialCacheProperty =
            DependencyProperty.Register(nameof(EnableCredentialCache), typeof(bool), typeof(SignInForm), new PropertyMetadata(true, OnEnableCredentialCachePropertyChanged));

        private static void OnEnableCredentialCachePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SignInForm)d;
            if (ctrl._rememberCredentialsButton != null)
            {
                ctrl._rememberCredentialsButton.Visibility = ctrl.EnableCredentialCache ? Visibility.Visible : Visibility.Collapsed;
                ctrl.PopulateFields();
            }
        }

        /// <summary>
        /// Gets or sets the text displayed in the OK button.
        /// </summary>
        public string? OkButtonText
        {
            get { return (string)GetValue(OkButtonTextProperty); }
            set { SetValue(OkButtonTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OkButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OkButtonTextProperty =
            DependencyProperty.Register(nameof(OkButtonText), typeof(string), typeof(SignInForm), new PropertyMetadata("OK"));

        /// <summary>
        /// Gets or sets the text displayed in the Cancel button.
        /// </summary>
        public string? CancelButtonText
        {
            get { return (string)GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CancelButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelButtonTextProperty =
            DependencyProperty.Register(nameof(CancelButtonText), typeof(string), typeof(SignInForm), new PropertyMetadata("Cancel"));

        /// <summary>
        /// Gets or sets the text displayed in the remember credentials checkbox.
        /// </summary>
        public string? RememberCredentialsText
        {
            get { return (string)GetValue(RememberCredentialsTextProperty); }
            set { SetValue(RememberCredentialsTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RememberCredentialsText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RememberCredentialsTextProperty =
            DependencyProperty.Register(nameof(RememberCredentialsText), typeof(string), typeof(SignInForm), new PropertyMetadata("Remember my credentials"));

        /// <summary>
        /// Gets or sets the header text.
        /// </summary>
        public string? HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="HeaderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(SignInForm), new PropertyMetadata("Login required"));
    }
}
