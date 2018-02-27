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

using System.Security.Cryptography.X509Certificates;
using Esri.ArcGISRuntime.Security;

namespace Esri.ArcGISRuntime.Toolkit.Authentication
{
    /// <summary>
    /// A helper class for picking a certificate from the Certificate Store
    /// </summary>
    public static class CertificateHelper
    {
        /// <summary>
        /// Requests the user to pick a certificate from the Current User's <see cref="StoreLocation"/>
        /// </summary>
        /// <param name="info">The Credential request info from the <see cref="AuthenticationManager"/></param>
        /// <returns>A certificate picked by the user</returns>
        public static CertificateCredential SelectCertificate(CredentialRequestInfo info)
        {
            return SelectCertificate(info, new X509Store(StoreName.My, StoreLocation.CurrentUser));
        }

        /// <summary>
        /// Requests the user to pick a certificate from the provided <see cref="X509Store"/>.
        /// </summary>
        /// <param name="info">The Credential request info from the <see cref="AuthenticationManager"/></param>
        /// <param name="x509Store">The certificate store</param>
        /// <returns>A certificate picked by the user</returns>
        public static CertificateCredential SelectCertificate(CredentialRequestInfo info, X509Store x509Store)
        {
            X509Certificate2 certSelected = null;
            x509Store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection col = x509Store.Certificates;
            X509Certificate2Collection sel = X509Certificate2UI.SelectFromCollection(col, "Login required", "Certificate required for " + info.ServiceUri, X509SelectionFlag.SingleSelection);

            if (sel.Count > 0)
            {
                X509Certificate2Enumerator en = sel.GetEnumerator();
                en.MoveNext();
                x509Store.Close();
                return new CertificateCredential(certSelected);
            }

            return null;
        }
    }
}
