using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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
