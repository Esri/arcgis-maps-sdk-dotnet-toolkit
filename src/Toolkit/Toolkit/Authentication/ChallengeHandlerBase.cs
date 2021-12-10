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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Security;
using AuthenticationManager = Esri.ArcGISRuntime.Security.AuthenticationManager;

namespace Esri.ArcGISRuntime.Toolkit.Authentication
{
    /// <summary>
    /// A common base class for handling authentication reqeusts from ArcGIS services.
    /// </summary>
    public abstract class ChallengeHandlerBase : IChallengeHandler
    {
        /// <inheritdoc/>
        public Task<Credential> CreateCredentialAsync(CredentialRequestInfo info)
        {
            return info.AuthenticationType switch
            {
                AuthenticationType.Certificate => OnCertificateAsync(info),
                AuthenticationType.NetworkCredential => OnUsernamePasswordAsync(info),
                AuthenticationType.Token => OnTokenAsync(info),
                AuthenticationType.Proxy => OnProxyAsync(info),
                _ => throw new NotSupportedException("Unsupported authentication type requested."),
            };
        }

        private async Task<Credential> OnCertificateAsync(CredentialRequestInfo info)
        {
            var certificate = await RequestCertificateAsync(info);
            return new CertificateCredential(certificate) { ServiceUri = info.ServiceUri! };
        }

        private async Task<Credential> OnUsernamePasswordAsync(CredentialRequestInfo info)
        {
            var credentials = await RequestUsernamePasswordAsync(info);
            return new ArcGISNetworkCredential
            {
                Credentials = credentials,
                ServiceUri = info.ServiceUri!,
            };
        }

        private Task<Credential> OnTokenAsync(CredentialRequestInfo info)
        {
            // test whether OAuth is requested for this server
            if (info.GenerateTokenOptions?.TokenAuthenticationType != TokenAuthenticationType.ArcGISToken)
            {
                return OnOAuthTokenAsync(info);
            }

            // cannot check GenerateTokenOptions, check server configuration
            var serverInfo = AuthenticationManager.Current.FindServerInfo(info.ServiceUri!);
            if (serverInfo != null && serverInfo.TokenAuthenticationType != TokenAuthenticationType.ArcGISToken)
            {
                return OnOAuthTokenAsync(info);
            }

            return OnArcGISTokenAsync(info);
        }

        private async Task<Credential> OnArcGISTokenAsync(CredentialRequestInfo info)
        {
            var credentials = await RequestUsernamePasswordAsync(info);

            // pass the username and password back to the AM to generate a token
            return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri!,
                credentials.UserName, credentials.Password, info.GenerateTokenOptions);
        }

        private async Task<Credential> OnOAuthTokenAsync(CredentialRequestInfo info)
        {
            if (AuthenticationManager.Current.OAuthAuthorizeHandler is null)
            {
                throw new InvalidOperationException("The OAuth authorize handler has not been configured. Cannot generate OAuth token.");
            }

            return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri!, info.GenerateTokenOptions);
        }

        private async Task<Credential> OnProxyAsync(CredentialRequestInfo info)
        {
            var credentials = await RequestUsernamePasswordAsync(info);
            return new ProxyCredential(info.ProxyServiceUri!, "Basic", credentials);
        }

        /// <summary>
        /// When overridden, allows the handler to request a usename/password credential.
        /// </summary>
        /// <param name="info">An object containing information about the authentication request.</param>
        /// <returns>
        /// A task representing the asynchronous credential request. The value of the task is a network credential.
        /// </returns>
        protected abstract Task<NetworkCredential> RequestUsernamePasswordAsync(CredentialRequestInfo info);

        /// <summary>
        /// When overridden, allows the handler to request a certificate credential.
        /// </summary>
        /// <param name="info">An object containing information about the authentication request.</param>
        /// <returns>
        /// A task representing the asynchronous credential request. The value of the task is a X.509 certificate.
        /// </returns>
        protected abstract Task<X509Certificate2> RequestCertificateAsync(CredentialRequestInfo info);
    }
}