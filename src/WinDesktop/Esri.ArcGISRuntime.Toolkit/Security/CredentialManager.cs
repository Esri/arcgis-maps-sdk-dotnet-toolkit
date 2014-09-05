using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
#if NETFX_CORE
#error "Intended for Desktop only"
#endif
using System.Security.Cryptography;
using Esri.ArcGISRuntime.Security;

namespace Esri.ArcGISRuntime.Toolkit.Security
{

    /// <summary>
    /// Helper class for managing the storage of credentials in the credential locker
    /// </summary>
    internal static class CredentialManager
    {
        private const string ResourcePrefix = "ArcGISRuntime "; // Prefix to disambiguate with PasswordCredentials that would be saved by the app itself

        // password is stored with a prefix depending on the type of credential, so we are able to reinstantiate the right credential
        private const string PasswordPrefix = "Password:";
        private const string OAuthRefreshTokenPrefix = "OAuthRefreshToken:";
        private const string OAuthAccessTokenPrefix = "OAuthAccessToken:";
        private const string NetworkCredentialPasswordPrefix = "NetworkCredentialPassword:";


        /// <summary>
        /// Adds a credential to the credential locker.
        /// </summary>
        /// <param name="credential">The credential to be added.</param>
        public static void AddCredential(Credential credential)
        {
            if (credential == null || string.IsNullOrEmpty(credential.ServiceUri))
                return;

            var serverInfo = IdentityManager.Current.FindServerInfo(credential.ServiceUri);
            var host = serverInfo == null ? credential.ServiceUri : serverInfo.ServerUri;

            string passwordValue = null;  // value stored as password in the password locker
            string userName = null;
            var oAuthTokenCredential = credential as OAuthTokenCredential;
            var arcGISTokenCredential = credential as ArcGISTokenCredential;
            var arcGISNetworkCredential = credential as ArcGISNetworkCredential;
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

            // Store the value in the password locker
			//if (passwordValue != null)
			//{
			//	var passwordVault = new PasswordVault();
			//	var resource = ResourcePrefix + host;
			//	// remove previous resource stored for the same host
			//	try // FindAllByResource throws an exception when no pc are stored
			//	{
			//		foreach (PasswordCredential pc in passwordVault.FindAllByResource(resource))
			//			passwordVault.Remove(pc);
			//	}
			//	catch {}

			//	passwordVault.Add(new PasswordCredential(resource, userName, passwordValue));
			//}
        }

        /// <summary>
        /// Removes all ArcGISRuntime credentials.
        /// </summary>
        /// <remark>Remove application credentials only.</remark>
        public static void RemoveAllCredentials()
        {
			//var passwordVault = new PasswordVault();
			//foreach (PasswordCredential passwordCredential in passwordVault.RetrieveAll())
			//{
			//	if (passwordCredential.Resource.StartsWith(ResourcePrefix))
			//		passwordVault.Remove(passwordCredential);
			//}
        }

        /// <summary>
        /// Retrieves all ArcGISRuntime credentials stored in the Credential Locker.
        /// </summary>
        public static IEnumerable<Credential>  RetrieveAll()
        {
			var credentials = new List<Credential>();
			//var passwordVault = new PasswordVault();
			//foreach (PasswordCredential passwordCredential in passwordVault.RetrieveAll().Where(pc => pc.Resource.StartsWith(ResourcePrefix)))
			//{
			//	Credential credential = null;
			//	passwordCredential.RetrievePassword();
			//	string userName = passwordCredential.UserName;
			//	string passwordValue = passwordCredential.Password; // value stored as password
			//	string serviceUrl = passwordCredential.Resource.Substring(ResourcePrefix.Length);

			//	// Create the credential depending on the type
			//	if (passwordValue.StartsWith(PasswordPrefix))
			//	{
			//		string password = passwordValue.Substring(PasswordPrefix.Length);
			//		credential = new ArcGISTokenCredential { ServiceUri = serviceUrl, UserName = userName, Password = password, Token = "dummy"}; // dummy to remove once the token will be refreshed pro actively
			//	}
			//	else if (passwordValue.StartsWith(OAuthRefreshTokenPrefix))
			//	{
			//		string refreshToken = passwordValue.Substring(OAuthRefreshTokenPrefix.Length);
			//		credential = new OAuthTokenCredential
			//		{
			//			ServiceUri = serviceUrl,
			//			UserName = userName,
			//			OAuthRefreshToken = refreshToken,
			//			Token = "dummy"
			//		};
			//	}
			//	else if (passwordValue.StartsWith(OAuthAccessTokenPrefix))
			//	{
			//		string token = passwordValue.Substring(OAuthAccessTokenPrefix.Length);
			//		credential = new OAuthTokenCredential
			//		{
			//			ServiceUri = serviceUrl,
			//			UserName = userName,
			//			Token = token,
			//		};
			//	}
			//	else if (passwordValue.StartsWith(NetworkCredentialPasswordPrefix))
			//	{
			//		string password = passwordValue.Substring(NetworkCredentialPasswordPrefix.Length);
			//		credential = new ArcGISNetworkCredential {ServiceUri = serviceUrl, Credentials = new NetworkCredential(userName, password)};
			//	}

			//	if (credential != null)
			//	{
			//		credentials.Add(credential);
			//	}
			//}
            return credentials;
        }

		public static void EncryptInMemoryData(byte[] Buffer, MemoryProtectionScope Scope)
		{
			if (Buffer.Length <= 0)
				throw new ArgumentException("Buffer");
			if (Buffer == null)
				throw new ArgumentNullException("Buffer");


			// Encrypt the data in memory. The result is stored in the same same array as the original data.
			ProtectedMemory.Protect(Buffer, Scope);

		}

		public static void DecryptInMemoryData(byte[] Buffer, MemoryProtectionScope Scope)
		{
			if (Buffer.Length <= 0)
				throw new ArgumentException("Buffer");
			if (Buffer == null)
				throw new ArgumentNullException("Buffer");


			// Decrypt the data in memory. The result is stored in the same same array as the original data.
			ProtectedMemory.Unprotect(Buffer, Scope);

		}

		public static byte[] CreateRandomEntropy()
		{
			// Create a byte array to hold the random value. 
			byte[] entropy = new byte[16];

			// Create a new instance of the RNGCryptoServiceProvider. 
			// Fill the array with a random value. 
			new RNGCryptoServiceProvider().GetBytes(entropy);

			// Return the array. 
			return entropy;


		}

		public static int EncryptDataToStream(byte[] Buffer, byte[] Entropy, DataProtectionScope Scope, Stream S)
		{
			if (Buffer.Length <= 0)
				throw new ArgumentException("Buffer");
			if (Buffer == null)
				throw new ArgumentNullException("Buffer");
			if (Entropy.Length <= 0)
				throw new ArgumentException("Entropy");
			if (Entropy == null)
				throw new ArgumentNullException("Entropy");
			if (S == null)
				throw new ArgumentNullException("S");

			int length = 0;

			// Encrypt the data in memory. The result is stored in the same same array as the original data. 
			byte[] encrptedData = ProtectedData.Protect(Buffer, Entropy, Scope);

			// Write the encrypted data to a stream. 
			if (S.CanWrite && encrptedData != null)
			{
				S.Write(encrptedData, 0, encrptedData.Length);

				length = encrptedData.Length;
			}

			// Return the length that was written to the stream.  
			return length;

		}

		public static byte[] DecryptDataFromStream(byte[] Entropy, DataProtectionScope Scope, Stream S, int Length)
		{
			if (S == null)
				throw new ArgumentNullException("S");
			if (Length <= 0)
				throw new ArgumentException("Length");
			if (Entropy == null)
				throw new ArgumentNullException("Entropy");
			if (Entropy.Length <= 0)
				throw new ArgumentException("Entropy");



			byte[] inBuffer = new byte[Length];
			byte[] outBuffer;

			// Read the encrypted data from a stream. 
			if (S.CanRead)
			{
				S.Read(inBuffer, 0, Length);

				outBuffer = ProtectedData.Unprotect(inBuffer, Entropy, Scope);
			}
			else
			{
				throw new IOException("Could not read the stream.");
			}

			// Return the length that was written to the stream.  
			return outBuffer;

		}


    }
}
