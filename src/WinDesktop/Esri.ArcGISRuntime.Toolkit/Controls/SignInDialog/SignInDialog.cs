// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Net;
using System.IO;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;
using Esri.ArcGISRuntime.Security;

#if NETFX_CORE
#error "Intended for Desktop only"
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// The SignInDialog Control challenges the user for a credential
    /// when trying to access secured ArcGIS services.
    /// The SignInDialog Control can manage Network or Token credential.
    /// </summary>
    /// <remarks>
    /// This control is designed to work with the <see cref="IdentityManager" /> and the <see cref="Security.SignInChallengeHandler"/>.
    /// </remarks>[TemplatePart(Name = "RichTextBoxMessage", Type = typeof(RichTextBox))]
    [TemplatePart(Name = "RichTextBoxErrorMessage", Type = typeof(RichTextBox))]
    internal class SignInDialog : Control, INotifyPropertyChanged // might be public
    {
        #region Constructors
        private RichTextBox _rtbMessage;
        private RichTextBox _rtbErrorMessage;
        private string _rtbMessageInitialXaml;
        private string _rtbErrorMessageInitialXaml;
        private TaskCompletionSource<Credential> _tcs;
        private long _requestID; // flag allowing the reuse of the same dialog after cancelling a request

        /// <summary>
        /// Initializes a new instance of the <see cref="SignInDialog"/> control.
        /// </summary>
        public SignInDialog()
        {
            GenerateCredentialCommand = new ActionCommand(async ()=> await GenerateCredentialAsync(), () => IsReady);
            CancelCommand = new ActionCommand(Cancel, () => _tcs != null);
            DataContext = this;
        }

        /// <summary>
        /// Static initialization for the <see cref="SignInDialog"/> control.
        /// </summary>
        static SignInDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SignInDialog),
                new FrameworkPropertyMetadata(typeof(SignInDialog)));
        }
        #endregion

        #region Override OnApplyTemplate
        /// <summary>
        /// When overridden in a derived class, is invoked whenever application
        /// code or internal processes (such as a rebuilding layout pass) call 
        /// <see cref="T:System.Windows.Controls.Control.ApplyTemplate"/>. In
        /// simplest terms, this means the method is called just before a UI
        /// element displays in an application.
        /// </summary>t
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _rtbMessage = GetTemplateChild("RichTextBoxMessage") as RichTextBox;
            _rtbErrorMessage = GetTemplateChild("RichTextBoxErrorMessage") as RichTextBox;
            if (_rtbMessage != null)
                _rtbMessageInitialXaml = XamlWriter.Save(_rtbMessage.Document);
            if (_rtbErrorMessage != null)
                _rtbErrorMessageInitialXaml = XamlWriter.Save(_rtbErrorMessage.Document);
            SetRichTextBoxMessage();
            SetRichTextBoxErrorMessage();

            // Set focus 
            var elt = GetTemplateChild("FirstFocus") as UIElement;
            if (elt != null)
                elt.Focus();
        }

        #endregion

        #region Public property CredentialRequestInfo
        private CredentialRequestInfo _credentialRequestInfo;
        /// <summary>
        /// Gets the information about the ArcGIS service that needs a credential for getting access to. This property is set by calling <see cref="CreateCredentialAsync"/>.
        /// </summary>
        /// <value>
        /// The credential request info.
        /// </value>
        public CredentialRequestInfo CredentialRequestInfo
        {
            get { return _credentialRequestInfo; }
            private set
            {
                const string propertyName = "CredentialRequestInfo";
                if (_credentialRequestInfo != value)
                {
                    _credentialRequestInfo = value;

                    SetRichTextBoxMessage();
                    SetRichTextBoxErrorMessage();
                    OnPropertyChanged(propertyName);
                    UpdateCanGenerate();
                }
            }
        }
        #endregion

        #region Public property UserName
        private string _userName = string.Empty;
        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName
        {
            get { return _userName; }
            set
            {
                const string propertyName = "UserName";
                if (_userName != value)
                {
                    _userName = value;

                    OnPropertyChanged(propertyName);
                    UpdateCanGenerate();
                    if (string.IsNullOrEmpty(value))
                        throw new ArgumentNullException();
                }
            }
        }
        #endregion

        #region Public property Password
        private string _password = string.Empty;
        /// <summary>
        /// Gets or sets the password used to get the token.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password
        {
            get { return _password; }
            set
            {
                const string propertyName = "Password";
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged(propertyName);
                    UpdateCanGenerate();

                    if (string.IsNullOrEmpty(value))
                        throw new ArgumentNullException();
                }
            }
        }
        #endregion

        #region Public Read-Only property IsBusy
        private bool _isBusy;
        /// <summary>
        /// Gets a value indicating whether this instance is busy getting a token.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is busy; otherwise, <c>false</c>.
        /// </value>
        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged("IsBusy");
                    UpdateCanGenerate();
                }
            }
        }
        #endregion

        #region Public Read-Only Property ErrorMessage
        private string _errorMessage;

        /// <summary>
        /// Gets the error that occured during the latest token generation.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    SetRichTextBoxErrorMessage();
                    OnPropertyChanged("ErrorMessage");
                }
            }
        }
        #endregion

        #region Dependency Property Title
        /// <summary>
        /// Gets or sets the title that can be displayed by the SignInDialog or by the container of the SignInDialog.
        /// </summary>
        /// <remarks>The default SignInDialog template doesn't display the Title.</remarks>
        /// <value>
        /// The title.
        /// </value>
        public string Title
        {
            // This is a DP so the value can be initialized in XAML allowing easy L10N
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SignInDialog), null);

        #endregion

        #region Public Property CredentialSaveOption

        private CredentialSaveOption _credentialSaveOption;

        /// <summary>
        /// Gets or sets the option that specifies the initial state of the dialog's Save Credential check
        /// box. The default value is clear (unchecked).
        /// </summary>
        public CredentialSaveOption CredentialSaveOption
        {
            get { return _credentialSaveOption; }
            set
            {
                if (_credentialSaveOption != value)
                {
                    _credentialSaveOption = value;
                    OnPropertyChanged("CredentialSaveOption");
                }
            }
        }
        #endregion

        /// <summary>
        /// Gets the generate credential command allowing to generate a credential asynchronously.
        /// </summary>
        /// <remarks>This command is generally executed on the 'OK' button.</remarks>
        public ICommand GenerateCredentialCommand { get; private set; }

        /// <summary>
        /// Gets the cancel command allowing to cancel the ongoing token request.
        /// </summary>
        public ICommand CancelCommand { get; private set; }

        #region DoSignIn static challenge method

        private Task<Credential> DoSignInInUIThread(CredentialRequestInfo credentialRequestInfo)
        {
            // Create the ChildWindow that contains the SignInDialog
            var signInDialog = this;
            var childWindow = new Window
            {
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.ToolWindow,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowState = WindowState.Normal,
                Content = signInDialog
            };

            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                try
                {
                    childWindow.Owner = Application.Current.MainWindow;
                }
                catch
                {
                    // May fire an exception when used inside an excel or powerpoint addins
                }
            }


            // Bind the Title so the ChildWindow Title is the SignInDialog title (that will be initialized later)
            DependencyProperty titleProperty = Window.TitleProperty;
            var binding = new Binding("Title") { Source = signInDialog };
            childWindow.SetBinding(titleProperty, binding);
            childWindow.Closed += (s, e) => signInDialog.Cancel(); // be sure the SignInDialog is deactivated when closing the childwindow using the X


            // initialize the task that gets the credential and then close the window
            var ts = TaskScheduler.FromCurrentSynchronizationContext();
            var doSignInTask = signInDialog.WaitForCredentialAsync(credentialRequestInfo).ContinueWith(task =>
            {
                childWindow.Close();
                return task.Result;
            }, ts); 

            // Show the window
            childWindow.ShowDialog();

            return doSignInTask;
        }

        #endregion

        #region IChallengeHandler interface:  CreateCredentialAsync

        /// <summary>
        /// Challenge handler leaveraging the SignInDialog in a child window dialog.
        /// </summary>
        public Task<Credential> CreateCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            return CompatUtility.ExecuteOnUIThread(() => DoSignInInUIThread(credentialRequestInfo));
        }

        private async Task<Credential> WaitForCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            Debug.Assert(credentialRequestInfo != null);
            Cancel(); // cancel previous task
            _tcs = new TaskCompletionSource<Credential>();
            UpdateCanCancel();
            CredentialRequestInfo = credentialRequestInfo;
            using (credentialRequestInfo.CancellationToken.Register(Cancel, true))
            {
                return await _tcs.Task;
            }
        }

        private void Cancel()
        {
            IsBusy = false;
            ErrorMessage = null;
            _requestID++;
            CredentialRequestInfo = null;
            if (_tcs != null)
            {
                _tcs.TrySetCanceled();
                _tcs = null;
            }
        }

        #endregion

        #region Credential Generation

        private async Task GenerateCredentialAsync()
        {
            ErrorMessage = null;
            Credential crd = _credentialRequestInfo != null && _credentialRequestInfo.AuthenticationType == AuthenticationType.Token
                ? await GenerateTokenCredentialAsync()
                : GenerateNetworkCredential();

            if (crd != null && _tcs != null)
            {
                _tcs.SetResult(crd);
                _tcs = null;
            }
        }

        // Create a network credential from username/password 
        private Credential GenerateNetworkCredential()
        {
            return IsReady ? new ArcGISNetworkCredential { Credentials = new NetworkCredential(UserName, Password) } : null;
        }

        // Token Generation
        private async Task<Credential> GenerateTokenCredentialAsync()
        {
            if (!IsReady)
                return null;
            IsBusy = true;
            long requestID = _requestID;
            Exception error = null;
            Credential credential;
            try
            {
                credential = await IdentityManager.Current.GenerateCredentialAsync(_credentialRequestInfo.ServiceUri, UserName, Password, _credentialRequestInfo.GenerateTokenOptions);
            }
            catch (Exception e)
            {
                credential = null;
                error = e;
            }
            if (requestID != _requestID || _tcs == null)
                return null; // No more the current request

            IsBusy = false;

            if (error != null)
            {
                // Display the error message and let the user try again
                string message = error.Message;
                if (string.IsNullOrEmpty(message) && error.InnerException != null)
                    message = error.InnerException.Message;
                message = Regex.Replace(message, "Error code '[0-9]*' : '([^']*)'", "$1"); // Remove Error code from the message
                ErrorMessage = message;
            }
            return credential;
        }
        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if (propertyChanged != null)
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Private helper methods to generate message in RichTextBox

        private void SetRichTextBoxMessage()
        {
            MakeReplacements(_rtbMessage, _rtbMessageInitialXaml);
        }

        private void SetRichTextBoxErrorMessage()
        {
            MakeReplacements(_rtbErrorMessage, _rtbErrorMessageInitialXaml);
        }

        private void MakeReplacements(RichTextBox richTextBox, string xaml)
        {
            if (richTextBox == null || string.IsNullOrEmpty(xaml) || CredentialRequestInfo == null)
                return;

            string url = _credentialRequestInfo.ServiceUri;

            if (!string.IsNullOrEmpty(url))
            {
                url = Regex.Replace(url, "\\?.*", "", RegexOptions.IgnoreCase); // remove query parameters
                string resourceName = GetResourceName(url);
                ServerInfo serverInfo = IdentityManager.Current.FindServerInfo(url);
                string server = serverInfo == null ? Regex.Match(url, "https?://[^/]*").ToString() : serverInfo.ServerUri;
                xaml = xaml.Replace("$RESOURCENAME", XamlEncode(resourceName));
                xaml = xaml.Replace("$URL", XamlEncode(url));
                xaml = xaml.Replace("$SERVER", XamlEncode(server));
            }
            xaml = xaml.Replace("$AUTHENTICATIONTYPE", _credentialRequestInfo.AuthenticationType.ToString());
            xaml = xaml.Replace("$ERRORMESSAGE", XamlEncode(ErrorMessage));

            string previousError = _credentialRequestInfo.Response != null ? _credentialRequestInfo.Response.ReasonPhrase : null;
            xaml = xaml.Replace("$PREVIOUSERROR", XamlEncode(previousError));
            var stringReader = new StringReader(xaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            richTextBox.Document = XamlReader.Load(xmlReader) as FlowDocument;
        }

        private static string XamlEncode(string inputStr)
        {
            return string.IsNullOrEmpty(inputStr)
                       ? inputStr
                       : inputStr.Replace("&", "&amp;")
                                 .Replace("<", "&lt;")
                                 .Replace(">", "&gt;")
                                 .Replace("\"", "&quot;")
                                 .Replace("'", "&apos;");
        }

        private static string GetResourceName(string url)
        {
            if (url.IndexOf("/rest/services", StringComparison.OrdinalIgnoreCase) > 0)
                return GetSuffix(url);

            url = Regex.Replace(url, "https?://[^/]*/", "");
            url = Regex.Replace(url, ".*/items/([^/]+).*", "$1");
            url = Regex.Replace(url, ".*/groups/([^/]+).*", "$1");
            url = Regex.Replace(url, ".*/users/([^/]+).*", "$1");
            return url;
        }

        private static string GetSuffix(string url)
        {
            url = Regex.Replace(url, "http.+/rest/services/?", "", RegexOptions.IgnoreCase);
            url = Regex.Replace(url, "(/(MapServer|GeocodeServer|GPServer|GeometryServer|ImageServer|NAServer|FeatureServer|GeoDataServer|GlobeServer|MobileServer|GeoenrichmentServer)).*", "$1", RegexOptions.IgnoreCase);
            return url;
        }
        #endregion

        // Private methods
        private bool IsReady
        {
            get { return !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password) && !IsBusy && CredentialRequestInfo != null && _tcs != null; }
        }

        private void UpdateCanGenerate()
        {
            ((ActionCommand)GenerateCredentialCommand).RaiseCanExecute();
        }

        private void UpdateCanCancel()
        {
            ((ActionCommand)CancelCommand).RaiseCanExecute();
        }
    }

    /// <summary>
 /// *FOR INTERNAL USE ONLY* Helper to execute a command when user types 'Enter' and to update the binding source when the text changes.
    /// </summary>
 /// <exclude/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class TextInputManager
    {
        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public static ICommand GetEnterCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(EnterCommandProperty);
        }

        /// <summary>
        /// Sets the command.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="value">The value.</param>
        public static void SetEnterCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(EnterCommandProperty, value);
        }

        /// <summary>
        /// Identifies the Command attached property.
        /// </summary>
        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.RegisterAttached("EnterCommand", typeof(ICommand), typeof(TextInputManager), new PropertyMetadata(OnEnterCommandChanged));

        private static void OnEnterCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox)
            {
                var textBox = d as TextBox;
                if (e.OldValue != null)
                    textBox.KeyUp -= KeyUpHandler;
                if (e.NewValue != null)
                    textBox.KeyUp += KeyUpHandler;
            }
            else if (d is PasswordBox)
            {
                var textBox = d as PasswordBox;
                if (e.OldValue != null)
                    textBox.KeyUp -= KeyUpHandler;
                if (e.NewValue != null)
                    textBox.KeyUp += KeyUpHandler;
            }
        }

        static void KeyUpHandler(object sender, KeyEventArgs e)
        {
            if (!(sender is DependencyObject))
                return;

            if (e.Key == Key.Enter)
            {
                ICommand command = GetEnterCommand((DependencyObject)sender);

                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                    e.Handled = true;
                }
            }
            else
            {
                var passwordBox = sender as PasswordBox;
                if (passwordBox != null)
                {
                    SetPasswordText(passwordBox, passwordBox.Password);
                }
            }
        }


        /// <summary>
        /// Identifies the PasswordText attached property.
        /// In WPF this a workaround for the Password property that is not a DP.
        /// </summary>
        public static readonly DependencyProperty PasswordTextProperty =
            DependencyProperty.RegisterAttached("PasswordText", typeof(string), typeof(TextInputManager), new PropertyMetadata(OnPasswordTextChanged));

        private static void OnPasswordTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (d is PasswordBox)
            {
                var textBox = d as PasswordBox;
                var value = (string) e.NewValue;
                if (textBox.Password != value)
                    textBox.Password = value;
            }
        }

        /// <summary>
        /// Gets the password text.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public static string GetPasswordText(DependencyObject obj)
        {
            return (string)obj.GetValue(PasswordTextProperty);
        }

        /// <summary>
        /// Sets the password text.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="value">The value.</param>
        public static void SetPasswordText(DependencyObject obj, string value)
        {
            obj.SetValue(PasswordTextProperty, value);
        }
    }

    /// <summary>
    /// *FOR INTERNAL USE ONLY* Convert a  CredentialSaveOption to a bool.
    /// </summary>
    /// <exclude/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class CredentialSaveOptionBoolConverter : IValueConverter
    {
        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CredentialSaveOption)
            {
                var credentialSaveOption = (CredentialSaveOption)value;
                return credentialSaveOption == CredentialSaveOption.Selected;
            }
            return null;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object. This method is called only in TwoWay bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                var val = (bool)value;
                return val ? CredentialSaveOption.Selected : CredentialSaveOption.Unselected;
            }
            return null;
        }
    }

    /// <summary>
    /// *FOR INTERNAL USE ONLY* Convert a  CredentialSaveOption to a visibility.
    /// </summary>
    /// <exclude/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class CredentialSaveOptionVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CredentialSaveOption)
            {
                var credentialSaveOption = (CredentialSaveOption)value;
                return credentialSaveOption == CredentialSaveOption.Hidden ? Visibility.Collapsed : Visibility.Visible;
            }
            return null;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object. This method is called only in TwoWay bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Identifies the state of the dialog box option on whether to save credentials.
    /// </summary>
    public enum CredentialSaveOption
    {
        /// <summary>
        /// The "Save credentials?" dialog box is not selected, indicating that the user doesn't want their credentials saved.
        /// </summary>
        Unselected = 0,

        /// <summary>
        /// The "Save credentials?" dialog box is selected, indicating that the user wants their credentials saved.
        /// </summary>
        Selected,

        /// <summary>
        /// The "Save credentials?" dialog box is not displayed at all.
        /// </summary>
        Hidden
    }

} 

