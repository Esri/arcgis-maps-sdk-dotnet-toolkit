using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Controls;

#if !WINDOWS_PHONE_APP
#error "Intended for WinPhone only"
#endif

namespace Esri.ArcGISRuntime.Toolkit.Security
{
    /// <summary>
    /// WinPhone component that handles the authorization errors returned by the requests to the ArcGIS resources.
    /// <para>
    /// This component is designed to work with the <see cref="IdentityManager" />.
    /// It can be initialized with code like:
    /// <code>
	/// IdentityManager.Current.ChallengeHandler = new Esri.ArcGISRuntime.Toolkit.Security.SignInChallengeHandler();
    /// </code>
    /// </para>
    /// <para/>
    /// Optionally, depending on the <see cref="AllowSaveCredentials"/> value, the credentials may be cached in the <see cref="PasswordVault"/> in a secure manner.
    /// In this case, the <see cref="PasswordVault"/> roams credentials to other Windows8 systems.
    /// <para/>
	/// By default the SignInChallengeHandler doesn't allow saving the Credentials. To allow it, the <see cref="SignInChallengeHandler"/> can be instantiated with code like:
    /// <code>
	///  IdentityManager.Current.ChallengeHandler = new Esri.ArcGISRuntime.Toolkit.Security.SignInChallengeHandler { AllowSaveCredentials = true, CredentialSaveOption = CredentialSaveOption.Selected  };
    /// </code>
    /// </summary>
    public class SignInChallengeHandler : IChallengeHandler
    {
        private bool _allowSaveCredentials;
        private bool _areCredentialsRestored;
        private readonly SignInDialog _signInDialog;
        private CredentialSaveOption _credentialSaveOption;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignInChallengeHandler"/> class.
        /// </summary>
        public SignInChallengeHandler() : this(new SignInDialog())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignInChallengeHandler"/> class.
        /// </summary>
        /// <param name="signInDialog">The underlying SignInDialog that will displayed inside a ContentDialog.</param>
        /// <exception cref="System.ArgumentNullException">signInDialog</exception>
        internal SignInChallengeHandler(SignInDialog signInDialog) // might be public
        {
            if (signInDialog == null)
                throw new ArgumentNullException("signInDialog");
            _signInDialog = signInDialog;
            SetSignInDialogCredentialSaveOption();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entered credentials may be saved in the credential locker.
        /// <para/>
        /// The first time AllowSaveCredentials is set to true, the cached credentials are added to the <see cref="IdentityManager.AddCredential">IdentityManager</see>
        /// </summary>
        /// <remark>The default value is false.</remark>
        public bool AllowSaveCredentials
        {
            get { return _allowSaveCredentials; }
            set
            {
                if (_allowSaveCredentials != value)
                {
                    _allowSaveCredentials = value;
                    if (_allowSaveCredentials && !_areCredentialsRestored)
                    {
                        // The first time AllowSaveCredentials is set to true, add the cached credentials to IM
                        _areCredentialsRestored = true;
                        foreach (var crd in RetrieveAllSavedCredentials())
                            IdentityManager.Current.AddCredential(crd);
                    }
                    SetSignInDialogCredentialSaveOption();
                }
            }
        }

        /// <summary>
        /// Gets or sets the option that specifies the initial state of the dialog's Save Credential check
        /// box. This property is ignored if <see cref="AllowSaveCredentials"/> is set to false.
        /// </summary>
        /// <para>If the value is Hidden, the user will have no choice to save the entered credential or not.
        /// The credential will always be saved if <see cref="AllowSaveCredentials"/> is set to true.</para>
        /// <remarks>The default value is Unselected.</remarks>
        public CredentialSaveOption CredentialSaveOption
        {
            get { return _credentialSaveOption; }
            set
            {
                if (_credentialSaveOption != value)
                {
                    _credentialSaveOption = value;
                    SetSignInDialogCredentialSaveOption();
                }
            }
        }

        private void SetSignInDialogCredentialSaveOption()
        {
            _signInDialog.CredentialSaveOption = AllowSaveCredentials
                ? CredentialSaveOption
                : CredentialSaveOption.Hidden;
        }

        /// <summary>
        /// Clears all ArcGISRuntime credentials from the Credential Locker.
        /// </summary>
        public void ClearCredentialsCache()
        {
            CredentialManager.RemoveAllCredentials();
        }

        /// <summary>
        /// Retrieves all ArcGISRuntime credentials stored in the Credential Locker.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Credential> RetrieveAllSavedCredentials()
        {
            return CredentialManager.RetrieveAll();
        }

        /// <summary>
        /// Challenge for getting the credential allowing to access to the specified ArcGIS resource.
        /// </summary>
        /// <param name="credentialRequestInfo">Information about the ArcGIS resource that needs a credential for getting access to.</param>
        /// <returns>a Task object with <see cref="Credential"/> upon successful completion. 
        /// Otherwise, the Task.Exception is set.</returns>
        public virtual Task<Credential> CreateCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            if (credentialRequestInfo == null)
                throw new ArgumentNullException("credentialRequestInfo");

            var serverInfo = IdentityManager.Current.FindServerInfo(credentialRequestInfo.ServiceUri);

            // Check if we need to use OAuth for login.
            // In this case we don't have to display the SignInDialog by ourself but we have to go through the OAuth authorization page
            bool isOauth = false;
            if (serverInfo != null && credentialRequestInfo.AuthenticationType == AuthenticationType.Token)
            {
                if (serverInfo.TokenAuthenticationType != TokenAuthenticationType.ArcGISToken)
                {
                    isOauth = true; // portal secured by OAuth
                }
                else if (!string.IsNullOrEmpty(serverInfo.OwningSystemUri))
                {
                    // server federated to OAuth portal?
                    // Check if the portal uses OAuth
                    isOauth = IdentityManager.Current.ServerInfos.Any(s => SameOwningSystem(s, serverInfo) && s.TokenAuthenticationType != TokenAuthenticationType.ArcGISToken);
                }
            }

            if (isOauth)
                // OAuth case --> call GenerateCredentialAsync (that will throw an exception if the OAuthAuthorize component is not set)
                return CreateOAuthCredentialAsync(credentialRequestInfo);

            // Display the sign in dialog and create a credential 
            return SignInDialogCreateCredentialAsync(credentialRequestInfo);
        }


        // Display the sign in dialog and create a credential 
        private async Task<Credential> SignInDialogCreateCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            Credential credential = await _signInDialog.CreateCredentialAsync(credentialRequestInfo);

            if (AllowSaveCredentials && _signInDialog.CredentialSaveOption != CredentialSaveOption.Unselected)
                CredentialManager.AddCredential(credential);
            return credential;
        }

        // get a OAuth token
        private async Task<Credential> CreateOAuthCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            TokenCredential credential = await IdentityManager.Current.GenerateCredentialAsync(credentialRequestInfo.ServiceUri, credentialRequestInfo.GenerateTokenOptions);
            if (AllowSaveCredentials)
                CredentialManager.AddCredential(credential);
            return credential;
        }

        private static bool SameOwningSystem(ServerInfo info1, ServerInfo info2)
        {
            string owningSystemUrl1 = info1.OwningSystemUri;
            string owningSystemUrl2 = info2.OwningSystemUri;
            if (owningSystemUrl1 == null || owningSystemUrl2 == null)
                return false;

            // test without taking care of the scheme
            owningSystemUrl1 = owningSystemUrl1.Replace("https:", "http:");
            owningSystemUrl2 = owningSystemUrl2.Replace("https:", "http:");
            return owningSystemUrl1 == owningSystemUrl2;
        }
    }
}
