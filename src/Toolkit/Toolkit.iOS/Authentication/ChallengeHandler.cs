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
using Esri.ArcGISRuntime.Toolkit.UI;

namespace Esri.ArcGISRuntime.Toolkit.Authentication
{
    /// <summary>
    /// A default challenge handler capable of responding to authentication requests from ArcGIS services.
    /// </summary>
    public class ChallengeHandler : ChallengeHandlerBase
    {
        /// <inheritdoc/>
        protected override async Task<NetworkCredential> RequestUsernamePasswordAsync(CredentialRequestInfo info)
        {
            var challenge = new UsernamePasswordChallenge(info);
            var result = await challenge.ShowAsync();
            if (result is null)
            {
                throw new InvalidOperationException("No credential was provided.");
            }

            return result;
        }

        /// <inheritdoc/>
        protected override Task<X509Certificate2> RequestCertificateAsync(CredentialRequestInfo info)
        {
            throw new PlatformNotSupportedException("Certificate authentication is not supported.");
        }
    }
}
