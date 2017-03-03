using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A Sign-in form for generating either an ArcGIS Token Credential or a Network Credential.
    /// </summary>
    public class SignInForm : Control
    {
        private ServerInfo serverInfo;
        private ToggleButton rememberCredentialsButton;
        private PasswordBox password;
        private TextBox username;
        private TextBlock messageText;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignInForm"/> control.
        /// </summary>
        public SignInForm()
        {
            DefaultStyleKey = typeof(SignInForm);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ButtonBase okButton = GetTemplateChild("OkButton") as ButtonBase;
            ButtonBase cancelButton = GetTemplateChild("CancelButton") as ButtonBase;
            rememberCredentialsButton = GetTemplateChild("RememberCredentials") as ToggleButton;
            if (rememberCredentialsButton != null)
            {
                rememberCredentialsButton.Visibility = EnableCredentialCache ? Visibility.Visible : Visibility.Collapsed;
            }

            username = GetTemplateChild("Username") as TextBox;
            password = GetTemplateChild("Password") as PasswordBox;
            messageText = GetTemplateChild("MessageText") as TextBlock;

            okButton.Click += OkButton_Click;
            cancelButton.Click += CancelButton_Click;

            PopulateFields();
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var info = CredentialRequestInfo;
            if(info == null)
            {
                return;
            }
            if (info.AuthenticationType == AuthenticationType.Token)
            {
                try
                {
                    Credential = await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, username.Text, ConvertToUnsecureString(password.SecurePassword), info.GenerateTokenOptions);
                }
                catch (System.Exception ex)
                {
                    CreateCredentialError?.Invoke(this, ex);
                    return;
                }
            }
            else if (info.AuthenticationType == AuthenticationType.NetworkCredential)
            {
                Credential = new ArcGISNetworkCredential() { Credentials = new System.Net.NetworkCredential(username.Text, password.SecurePassword) };
            }
            else
            {
                CreateCredentialError?.Invoke(this, new NotSupportedException("Authentication type not supported"));
            }
            if (EnableCredentialCache)
            {
                var host = serverInfo == null ? info.ServiceUri : serverInfo.ServerUri;
                if (rememberCredentialsButton != null && rememberCredentialsButton.IsChecked.Value)
                {
                    Authentication.CredentialsCache.SaveCredential(username.Text, password.SecurePassword, host);
                }
                else
                {
                    Authentication.CredentialsCache.DeleteCredential(host);
                }
            }
            Completed?.Invoke(this, Credential);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateFields()
        {
            if (rememberCredentialsButton != null)
                rememberCredentialsButton.IsChecked = false;
            if (username != null)
                username.Text = string.Empty;
            if (password != null)
                password.Password = string.Empty;

            if (ServerHost != null && EnableCredentialCache)
            {
                var credential = Authentication.CredentialsCache.ReadCredential(ServerHost);
                if (credential != null)
                {
                    if (username != null)
                        username.Text = credential.Item1;
                    if (password != null)
                        password.Password = ConvertToUnsecureString(credential.Item2);
                    if (rememberCredentialsButton != null)
                        rememberCredentialsButton.IsChecked = true;
                }
            }
        }

        /// <summary>
        /// Raised when the cancel button is pressed
        /// </summary>
        public event EventHandler Cancelled;

        /// <summary>
        /// Raised when a credential has successfully been created
        /// </summary>
        public event EventHandler<Credential> Completed;

        /// <summary>
        /// Raised it the form failed to generate a credential from the supplied username and password
        /// </summary>
        public event EventHandler<Exception> CreateCredentialError;

        /// <summary>
        /// Gets the credential generated by the dialog
        /// </summary>
        public Credential Credential { get; private set; }

        private static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                return string.Empty;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
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
        public Uri ServerHost
        {
            get { return (Uri)GetValue(ServerHostPropertyKey.DependencyProperty); }
        }

        private static readonly DependencyPropertyKey ServerHostPropertyKey =
            DependencyProperty.RegisterReadOnly("ServerHost", typeof(Uri), typeof(SignInForm), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ServerHost"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ServerHostProperty = ServerHostPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets or sets the message displayed to the user.
        /// </summary>
        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MessageText"/> dependency property
        /// </summary>
        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register(nameof(MessageText), typeof(string), typeof(SignInForm), new PropertyMetadata("Please enter credentials for"));
        
        /// <summary>
        /// Gets or sets the credential info used to generated a credential from.
        /// </summary>
        public CredentialRequestInfo CredentialRequestInfo
        {
            get { return (CredentialRequestInfo)GetValue(CredentialRequestInfoProperty); }
            set { SetValue(CredentialRequestInfoProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OkButtonText"/> dependency property
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
            if (info.AuthenticationType == AuthenticationType.Certificate)
            {
                //Use Authentication.CertificateHelper.SelectCertificate instead
                throw new NotSupportedException("Certificate Authentication not supported by this control");
            }
            Uri serverHost = null;
            serverInfo = null;
            if (info != null)
            {
                serverInfo = AuthenticationManager.Current.FindServerInfo(info.ServiceUri);
                serverHost = serverInfo == null ? info.ServiceUri : serverInfo.ServerUri;
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
        /// Identifies the <see cref="EnableCredentialCache"/> dependency property
        /// </summary>
        public static readonly DependencyProperty EnableCredentialCacheProperty =
            DependencyProperty.Register(nameof(EnableCredentialCache), typeof(bool), typeof(SignInForm), new PropertyMetadata(true, OnEnableCredentialCachePropertyChanged));

        private static void OnEnableCredentialCachePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SignInForm)d;
            if(ctrl.rememberCredentialsButton  != null)
            {
                ctrl.rememberCredentialsButton.Visibility = ctrl.EnableCredentialCache ? Visibility.Visible : Visibility.Collapsed;
                ctrl.PopulateFields();
            }
        }

        /// <summary>
        /// Gets or sets the text displayed in the OK button
        /// </summary>
        public string OkButtonText
        {
            get { return (string)GetValue(OkButtonTextProperty); }
            set { SetValue(OkButtonTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OkButtonText"/> dependency property
        /// </summary>
        public static readonly DependencyProperty OkButtonTextProperty =
            DependencyProperty.Register(nameof(OkButtonText), typeof(string), typeof(SignInForm), new PropertyMetadata("OK"));

        /// <summary>
        /// Gets or sets the text displayed in the Cancel button
        /// </summary>
        public string CancelButtonText
        {
            get { return (string)GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CancelButtonText"/> dependency property
        /// </summary>
        public static readonly DependencyProperty CancelButtonTextProperty =
            DependencyProperty.Register(nameof(CancelButtonText), typeof(string), typeof(SignInForm), new PropertyMetadata("Cancel"));

        /// <summary>
        /// Gets or sets the text displayed in the remember credentials checkbox
        /// </summary>
        public string RememberCredentialsText
        {
            get { return (string)GetValue(RememberCredentialsTextProperty); }
            set { SetValue(RememberCredentialsTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RememberCredentialsText"/> dependency property
        /// </summary>
        public static readonly DependencyProperty RememberCredentialsTextProperty =
            DependencyProperty.Register(nameof(RememberCredentialsText), typeof(string), typeof(SignInForm), new PropertyMetadata("Remember my credentials"));

        /// <summary>
        /// Gets or sets the header text
        /// </summary>
        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="HeaderText"/> dependency property
        /// </summary>
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(SignInForm), new PropertyMetadata("Login required"));

    }
}
