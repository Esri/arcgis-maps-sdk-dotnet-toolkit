using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System.Threading.Tasks;
using System.Windows;

namespace Esri.ArcGISRuntime.Toolkit.Authentication
{
    /// <summary>
    /// Custom <see cref="AuthenticationManager"/> Challenge Handler that combines user/password, certificate and OAuth authentication
    /// </summary>
    public class ChallengeHandler : Esri.ArcGISRuntime.Security.IChallengeHandler
    {
        private System.Windows.Threading.Dispatcher _dispatcher;

        /// <summary>
        /// Initializes a new instance of the challenge handler
        /// </summary>
        /// <param name="dispatcher">The Dispatcher for the application</param>
        public ChallengeHandler(System.Windows.Threading.Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Gets or sets a value indicating whether credentials should be stored in the system's Credentials Cache
        /// </summary>
        public bool EnableCredentialsCache { get; set; } = true;

        /// <summary>
        /// Gets or sets the style applied to the SignInForm
        /// </summary>
        public Style SignInFormStyle { get; set; }

        /// <inheritdoc cref="Esri.ArcGISRuntime.Security.IChallengeHandler.CreateCredentialAsync(CredentialRequestInfo)" />
        public Task<Credential> CreateCredentialAsync(CredentialRequestInfo info)
        {
            TaskCompletionSource<Credential> tcs = new TaskCompletionSource<Credential>();
            _dispatcher.InvokeAsync(() =>
            {
                if (info.AuthenticationType == AuthenticationType.Certificate)
                {
                    var cert = Authentication.CertificateHelper.SelectCertificate(info);
                    tcs.SetResult(cert);
                }
                else
                {
                    var login = CreateWindow(info);
                    login.Closed += (s, e) =>
                    {
                        tcs.SetResult((login.Content as SignInForm).Credential);
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
                ResizeMode = ResizeMode.NoResize
            };
            var contentDialog = new SignInForm()
            {
                EnableCredentialCache = EnableCredentialsCache,
                CredentialRequestInfo = info
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
