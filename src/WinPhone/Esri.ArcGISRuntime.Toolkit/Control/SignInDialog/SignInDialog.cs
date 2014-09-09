// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Esri.ArcGISRuntime.Security;

#if !WINDOWS_PHONE_APP
#error "Intended for WinPhone only"
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
    /// </remarks>
    internal class SignInDialog : Control, INotifyPropertyChanged // might be public later
    {
        #region Constructors
        private TaskCompletionSource<Credential> _tcs;
        private long _requestID; // flag allowing the reuse of the same dialog after cancelling a request

        /// <summary>
        /// Initializes a new instance of the <see cref="SignInDialog"/> control.
        /// </summary>
        public SignInDialog()
        {
            DefaultStyleKey = typeof(SignInDialog);
        }
        #endregion

        #region CredentialRequestInfo
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
                if (_credentialRequestInfo != value)
                {
                    _credentialRequestInfo = value;

                    OnPropertyChanged("CredentialRequestInfo");
                    OnPropertyChanged("IsReady");
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
                if (_userName != value)
                {
                    _userName = value;

                    OnPropertyChanged("UserName");
                    OnPropertyChanged("IsReady");
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
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged("Password");
                    OnPropertyChanged("IsReady");
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
                    OnPropertyChanged("IsReady");
                }
            }
        }
        #endregion

        #region Public Read-Only property IsReady
        /// <summary>
        /// Gets a value indicating whether this instance is ready for generating a token.
        /// </summary>
        public bool IsReady
        {
            get { return !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password) && !IsBusy && CredentialRequestInfo != null && _tcs != null; }
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
                    OnPropertyChanged("ErrorMessage");
                }
            }
        }
        #endregion

        #region Dependency Property Title
        /// <summary>
        /// Gets or sets the title of the ContentDialog encapsulating the SignInDialog.
        /// </summary>
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

        #region IChallengeHandler interface:  CreateCredentialAsync

        /// <summary>
        /// Challenge handler leaveraging the SignInDialog in a content dialog.
        /// </summary>
        public Task<Credential> CreateCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            return CompatUtility.ExecuteOnUIThread(() => DoSignInInUIThread(credentialRequestInfo));
        }

        private async Task<Credential> DoSignInInUIThread(CredentialRequestInfo credentialRequestInfo)
        {
            var signInDialog = this;

            // Create the ContentDialog that contains the SignInDialog
            var contentDialog = new ContentDialog
            {
                SecondaryButtonText = "cancel",
                PrimaryButtonText = "sign in",
                FullSizeDesired = true,
                Content = signInDialog,
            };

            contentDialog.PrimaryButtonClick += ContentDialogOnPrimaryButtonClick;

            // Bind the Title so the ContentDialog Title is the SignInDialog title (that will be initialized later)
            contentDialog.SetBinding(ContentDialog.TitleProperty, new Binding { Path = new PropertyPath("Title"), Source = signInDialog });

            contentDialog.SetBinding(ContentDialog.IsPrimaryButtonEnabledProperty, new Binding { Path = new PropertyPath("IsReady"), Source = signInDialog });

            contentDialog.Closed += ContentDialogOnClosed; // be sure the SignInDialog is deactivated when pressing back button

            // Show the content dialog
            var _ = contentDialog.ShowAsync(); // don't await else that would block here

            // Wait for the creation of the credential
            Credential crd;
            try
            {
                crd = await signInDialog.WaitForCredentialAsync(credentialRequestInfo);
            }
            finally
            {
                // Close the content dialog
                contentDialog.Hide();
            }

            return crd;
        }


        private async Task<Credential> WaitForCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            Debug.Assert(credentialRequestInfo != null);
            Cancel(); // cancel previous task
            _tcs = new TaskCompletionSource<Credential>();
            CredentialRequestInfo = credentialRequestInfo; // Will update 'IsReady' status
            //using (credentialRequestInfo.CancellationToken.Register(Cancel, true)); // for any reason, I get a crash if using true as last argument (whatever the callback, WinPhone bug?)
            using (credentialRequestInfo.CancellationToken.Register(() =>
                                                                    {
                                                                        if (_tcs != null)
                                                                        {
                                                                            _tcs.TrySetCanceled();
                                                                            _tcs = null;
                                                                        }
                                                                    }, false))
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

        private void ContentDialogOnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // try to generate the credential
            var _ = GenerateCredentialAsync();
            args.Cancel = true; // don't close the dialog for now, wait for the generation result
        }

        private void ContentDialogOnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            sender.Closed -= ContentDialogOnClosed;
            var signInDialog = sender.Content as SignInDialog;
            if (signInDialog != null)
            {
                signInDialog.Cancel(); // be sure the SignInDialog is deactivated when pressing back button
                sender.Content = null;
            }
        }

        #endregion

        #region Credential Generation

        private async Task<Credential> GenerateCredentialAsync()
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
            return crd;
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

    }

    /// <summary>
    /// *FOR INTERNAL USE ONLY* Convert a CredentialRequestInfo to a message for the SignInDialog.
    /// </summary>
    /// <exclude/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class SignInDialogMessageConverter : IValueConverter
    {

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var credentialRequestInfo = value as CredentialRequestInfo;
            if (credentialRequestInfo != null)
            {
                if (targetType == typeof (string))
                {
                    string server = credentialRequestInfo.ServiceUri;
                    int index = server.IndexOf("/rest/services", StringComparison.OrdinalIgnoreCase);

                    if (index < 0)
                        index = server.IndexOf("/sharing", StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        index = server.LastIndexOf('/');
                    if (index > 0)
                        server = server.Substring(0, index);

                    string message = string.Format("Enter account information to sign in to {0}", server);
                    if (credentialRequestInfo.AuthenticationType == AuthenticationType.NetworkCredential)
                        message += " (NetworkCredential Authentication).";
                    else
                        message += ".";
                    return message;
                }
            }

            return null;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object. This method is called only in TwoWay bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
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
        /// <param name="language">The language of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, string language)
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
        /// <param name="language">The language of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
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
        /// <param name="language">The language of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, string language)
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
        /// <param name="language">The language of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
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

