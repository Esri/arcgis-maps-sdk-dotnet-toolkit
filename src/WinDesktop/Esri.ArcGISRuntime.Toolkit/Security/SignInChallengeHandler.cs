// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Controls;

#if NETFX_CORE
#error "Intended for Desktop only"
#endif

namespace Esri.ArcGISRuntime.Toolkit.Security
{
    /// <summary>
    /// Desktop component that handles the authorization errors returned by the requests to the ArcGIS resources.
    /// <para>
    /// This component is designed to work with the <see cref="IdentityManager" />.
    /// It can be initialized with code like:
    /// <code>
    /// IdentityManager.Current.ChallengeHandler = new Esri.ArcGISRuntime.Toolkit.Security.SignInChallengeHandler();
    /// </code>
    /// </para>
    /// <para/>
    /// Optionally, depending on the <see cref="AllowSaveCredentials"/> value, the credentials may be cached in the <see cref="IsolatedStorage"/> in a secure manner.
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
            CredentialSaveOption = CredentialSaveOption.Hidden; // default value
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
            _signInDialog.CredentialSaveOption = CredentialSaveOption;
        }

        /// <summary>
        /// Clears all ArcGISRuntime credentials from the Credential Locker.
        /// </summary>
        public void ClearCredentialsCache()
        {
            CredentialManager.RemoveAllCredentials();
        }

        /// <summary>
        /// Retrieves all ArcGISRuntime credentials stored in the isolated storage.
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

            if (credentialRequestInfo.AuthenticationType == AuthenticationType.Certificate)
            {
                // Challenge for a certificate
                return CompatUtility.ExecuteOnUIThread(()=>CreateCertificateCredentialAsync(credentialRequestInfo));
            }

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
            CredentialSaveOption = _signInDialog.CredentialSaveOption; // so a custom component can use it
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


        private Task<Credential> CreateCertificateCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            var tcs = new TaskCompletionSource<Credential>();
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            X509Certificate2Collection certificates;
            try
            {
                const string clientAuthOid = "1.3.6.1.5.5.7.3.2"; // Client Authentication OID
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                // Find Client Authentication certificate
                certificates = store.Certificates.Find(X509FindType.FindByApplicationPolicy, clientAuthOid, true);
            }
            catch (Exception)
            {
                certificates = null;
            }
            finally
            {
                store.Close();
            }

            string url = credentialRequestInfo.ServiceUri;
            ServerInfo serverInfo = IdentityManager.Current.FindServerInfo(url);
            if (certificates != null && certificates.Count >= 1)
            {
                // Let the user select/validate the certificate
                string resourceName = GetResourceName(url);
                string server = serverInfo == null ? Regex.Match(url, "http.?//[^/]*").ToString() : serverInfo.ServerUri;
                string message = resourceName == null
                    ? string.Format("certificate required to access to {0}", server)
                    : string.Format("certificate required to access {0} on {1}", resourceName, server);
                certificates = X509Certificate2UI.SelectFromCollection(certificates, null, message, X509SelectionFlag.SingleSelection);
            }

            if (certificates != null && certificates.Count > 0)
            {
                var credential = new CertificateCredential(certificates[0]) { ServiceUri = serverInfo == null ? url : serverInfo.ServerUri };
                if (AllowSaveCredentials)
                    CredentialManager.AddCredential(credential);

                tcs.TrySetResult(credential);
            }
            else
            {
                // Note : Error type is not that important since the error returned to the user is the initial HTTP error (Authorization Error)
                tcs.TrySetException(new System.Security.Authentication.AuthenticationException());
            }
            return tcs.Task;
        }


        private static string GetResourceName(string url)
        {
            if (url.IndexOf("/rest/services", StringComparison.OrdinalIgnoreCase) > 0)
                return GetSuffix(url);

            return null;
        }

        private static string GetSuffix(string url)
        {
            url = Regex.Replace(url, "http.+/rest/services/?", "", RegexOptions.IgnoreCase);
            url = Regex.Replace(url, "(/(MapServer|GeocodeServer|GPServer|GeometryServer|ImageServer|NAServer|FeatureServer|GeoDataServer|GlobeServer|MobileServer|GeoenrichmentServer)).*", "$1", RegexOptions.IgnoreCase);
            return url;
        }
    }
}
