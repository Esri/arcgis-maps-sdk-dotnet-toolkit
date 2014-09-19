// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.IO.IsolatedStorage;
using System.Linq;
using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#if NETFX_CORE
#error "Intended for Desktop only"
#endif

namespace Esri.ArcGISRuntime.Toolkit.Security
{

    /// <summary>
    /// Helper class for managing the storage of credentials in the isolated storage in a secure manner
    /// </summary>
    internal static class CredentialManager
    {
        // password is stored with a prefix depending on the type of credential, so we are able to reinstantiate the right credential
        private const string PasswordPrefix = "Password:";
        private const string OAuthRefreshTokenPrefix = "OAuthRefreshToken:";
        private const string OAuthAccessTokenPrefix = "OAuthAccessToken:";
        private const string NetworkCredentialPasswordPrefix = "NetworkCredentialPassword:";
        private const string CertificateCredentialPrefix = "Certificate:";


        /// <summary>
        /// Adds a credential to the isolated storage.
        /// </summary>
        /// <param name="credential">The credential to be added.</param>
        public static void AddCredential(Credential credential)
        {
            if (credential == null || string.IsNullOrEmpty(credential.ServiceUri))
                return;

            var serverInfo = IdentityManager.Current.FindServerInfo(credential.ServiceUri);
            var host = serverInfo == null ? credential.ServiceUri : serverInfo.ServerUri;

            string passwordValue = null;  // value stored as password in the isolated storage
            string userName = null;
            var oAuthTokenCredential = credential as OAuthTokenCredential;
            var arcGISTokenCredential = credential as ArcGISTokenCredential;
            var arcGISNetworkCredential = credential as ArcGISNetworkCredential;
            var certificateCredential = credential as CertificateCredential;
            if (oAuthTokenCredential != null)
            {
                userName = oAuthTokenCredential.UserName;
                if (!string.IsNullOrEmpty(oAuthTokenCredential.OAuthRefreshToken)) // refreshable OAuth token --> we store it so we'll be able to generate a new token from it
                    passwordValue = OAuthRefreshTokenPrefix + oAuthTokenCredential.OAuthRefreshToken;
                else if (!string.IsNullOrEmpty(oAuthTokenCredential.Token))
                    passwordValue = OAuthAccessTokenPrefix + oAuthTokenCredential.Token;
            }
            else if (arcGISTokenCredential != null)
            {
                userName = arcGISTokenCredential.UserName;
                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(arcGISTokenCredential.Password)) // Token generated from an username/password --> store the password
                    passwordValue = PasswordPrefix + arcGISTokenCredential.Password;
            }
            else if (arcGISNetworkCredential != null)
            {
                // networkcredential: store the password
                if (arcGISNetworkCredential.Credentials != null)
                {
                    NetworkCredential networkCredential = arcGISNetworkCredential.Credentials.GetCredential(new Uri(host), "");
                    if (networkCredential != null && !string.IsNullOrEmpty(networkCredential.Password))
                    {
                        userName = networkCredential.UserName;
                        if (!string.IsNullOrEmpty(networkCredential.Domain))
                            userName = networkCredential.Domain + "\\" + userName;
                        passwordValue = NetworkCredentialPasswordPrefix + networkCredential.Password;
                    }
                }
            }
            else if (certificateCredential != null)
            {
                // certificateCredential: store the serial number
                if (certificateCredential.ClientCertificate != null)
                {
                    passwordValue = CertificateCredentialPrefix + certificateCredential.ClientCertificate.GetSerialNumberString();
                }
            }

            // Store the value in the isolated storage
            if (passwordValue != null)
            {
                AddCachedCredential(host, userName, passwordValue);
            }
        }




        /// <summary>
        /// Removes all ArcGISRuntime credentials.
        /// </summary>
        /// <remark>Remove application credentials only.</remark>
        public static void RemoveAllCredentials()
        {
            RemoveCachedCredentials();
        }

        /// <summary>
        /// Retrieves all ArcGISRuntime credentials stored in the isolated storage.
        /// </summary>
        public static IEnumerable<Credential>  RetrieveAll()
        {
            var credentials = new List<Credential>();
            foreach(var cachedCredential in GetCachedCredentials())
            {
                Credential credential = null;
                string userName = cachedCredential.UserName;
                string passwordValue = cachedCredential.Password; // value stored as password
                string serviceUrl = cachedCredential.Url;

                // Create the credential depending on the type
                if (passwordValue.StartsWith(PasswordPrefix))
                {
                    string password = passwordValue.Substring(PasswordPrefix.Length);
                    credential = new ArcGISTokenCredential { ServiceUri = serviceUrl, UserName = userName, Password = password, Token = "dummy" }; // dummy to remove once the token will be refreshed pro actively
                }
                else if (passwordValue.StartsWith(OAuthRefreshTokenPrefix))
                {
                    string refreshToken = passwordValue.Substring(OAuthRefreshTokenPrefix.Length);
                    credential = new OAuthTokenCredential
                    {
                        ServiceUri = serviceUrl,
                        UserName = userName,
                        OAuthRefreshToken = refreshToken,
                        Token = "dummy"
                    };
                }
                else if (passwordValue.StartsWith(OAuthAccessTokenPrefix))
                {
                    string token = passwordValue.Substring(OAuthAccessTokenPrefix.Length);
                    credential = new OAuthTokenCredential
                    {
                        ServiceUri = serviceUrl,
                        UserName = userName,
                        Token = token,
                    };
                }
                else if (passwordValue.StartsWith(NetworkCredentialPasswordPrefix))
                {
                    string password = passwordValue.Substring(NetworkCredentialPasswordPrefix.Length);
                    credential = new ArcGISNetworkCredential { ServiceUri = serviceUrl, Credentials = new NetworkCredential(userName, password) };
                }
                else if (passwordValue.StartsWith(CertificateCredentialPrefix))
                {
                    string serial = passwordValue.Substring(CertificateCredentialPrefix.Length);
                    var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    X509Certificate2Collection certificates;
                    try
                    {
                        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                        // Find certificate by serial number
                        certificates = store.Certificates.Find(X509FindType.FindBySerialNumber, serial, true);
                    }
                    catch (Exception)
                    {
                        certificates = null;
                    }
                    finally
                    {
                        store.Close();
                    }
                    if (certificates != null && certificates.Count > 0)
                        credential = new CertificateCredential(certificates[0]) { ServiceUri = serviceUrl };
                }

                if (credential != null)
                {
                    credentials.Add(credential);
                }
            }
            return credentials;
        }

 
        // Isolated storage private methods
        private const string CredentialFilePath = "ArcGISRuntimeCredentials"; // File created in the isolated storage

        // gets the IsolatedStorageFile
        private static IsolatedStorageFile GetStore()
        {
            return IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
        }

        // Removes all cached credentials
        private static void RemoveCachedCredentials()
        {
            try
            {
                using (var isoStore = GetStore())
                {
                    if (isoStore.FileExists(CredentialFilePath))
                        isoStore.DeleteFile(CredentialFilePath);
                }
            }
            catch
            {
            }
        }

        // gets cached credentials from the isolated storage
        private static IEnumerable<CachedCredential> GetCachedCredentials()
        {
            var cachedCredentials = new List<CachedCredential>();
            try
            {
                using (var isoStore = GetStore())
                {
                    if (isoStore.FileExists(CredentialFilePath))
                    {
                        using (var reader = new StreamReader(isoStore.OpenFile(CredentialFilePath, FileMode.Open, FileAccess.Read)))
                        {
                            string url;
                            while (!string.IsNullOrEmpty(url = reader.ReadLine()))
                            {
                                var credentialStorage = new CachedCredential { Url = url, UserName = reader.ReadLine(), Password = Decrypt(reader.ReadLine()) };
                                // remove duplicates
                                foreach (var sc in cachedCredentials.Where(c => c.Url == url).ToArray())
                                    cachedCredentials.Remove(sc);

                                cachedCredentials.Add(credentialStorage);
                            }
                        }
                    }
                }
            }
            catch { }
            return cachedCredentials;
        }

        // Adds a credential to the cache
        private static void AddCachedCredential(string host, string userName, string passwordValue)
        {
            try
            {
                string secret = Encrypt(passwordValue);

                using (var isoStore = GetStore())
                {
                    using (var writer = new StreamWriter(isoStore.OpenFile(CredentialFilePath, FileMode.Append, FileAccess.Write)))
                    {
                        writer.WriteLine(host);
                        writer.WriteLine(userName ?? "");
                        writer.WriteLine(secret);
                    }
                }
            }
            catch
            {
            }
        }

        private class CachedCredential
        {
            public string Url { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }


        // Encrypt and decrypt private methods
        private static string Encrypt(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);

            // Protect buffer must be 16 bytes in length or in multiples of 16 bytes. 
            int adjustedSize = 16*((buffer.Length - 1)/16 +1);
            if (adjustedSize != buffer.Length)
            {
                // Add padding null bytes
                var bytes = new List<byte>(buffer);
                bytes.AddRange(Enumerable.Repeat((byte)0, adjustedSize - buffer.Length));
                buffer = bytes.ToArray();
            }

            // Encrypt the data in memory. The result is stored in the same same array as the original data.
            ProtectedMemory.Protect(buffer, MemoryProtectionScope.SameLogon);
            return Convert.ToBase64String(buffer);
        }

        private static string Decrypt(string str)
        {
            byte[] buffer = Convert.FromBase64String(str);
            // Decrypt the data in memory. The result is stored in the same same array as the original data.
            ProtectedMemory.Unprotect(buffer, MemoryProtectionScope.SameLogon);

            // Remove padding null bytes
            int size = buffer.Length;
            while (size > 0 && buffer[size - 1] == 0)
                size--;
            return System.Text.Encoding.UTF8.GetString(buffer, 0, size);
        }
    }
}
