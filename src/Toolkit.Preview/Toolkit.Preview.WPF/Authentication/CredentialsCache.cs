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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Preview.Authentication
{
    /// <summary>
    /// A helper class for stoting credentials in the cache.
    /// </summary>
    public static class CredentialsCache
    {
        /// <summary>
        /// A credential retrieved from the credentials cache.
        /// </summary>
        public sealed class CachedCredential
        {
            internal CachedCredential(string username, SecureString password)
            {
                Username = username;
                Password = password;
            }

            /// <summary>
            /// Gets the username.
            /// </summary>
            public string Username { get; }

            /// <summary>
            /// Gets the password.
            /// </summary>
            public SecureString Password { get; }
        }

        private static string AppNamePrefix()
        {
            if (System.Windows.Application.Current != null)
            {
                return System.Windows.Application.Current.GetType().FullName + "|";
            }
            else
            {
                return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name + "|";
            }
        }

        /// <summary>
        /// Saves a credential to the credentials cache.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">The password.</param>
        /// <param name="serviceUri">The endpoint to associate the credentials with.</param>
        public static void SaveCredential(string username, SecureString password, Uri serviceUri)
        {
            string host = serviceUri?.OriginalString ?? throw new ArgumentNullException(nameof(serviceUri));
            CredentialManager.WriteCredential(AppNamePrefix() + host, username ?? throw new ArgumentNullException(nameof(username)), password ?? throw new ArgumentNullException(nameof(password)));
        }

        /// <summary>
        /// Removes a credential from the cache.
        /// </summary>
        /// <param name="serviceUri">Service Uri.</param>
        public static void DeleteCredential(Uri serviceUri)
        {
            string host = serviceUri?.OriginalString ?? throw new ArgumentNullException(nameof(serviceUri));
            CredentialManager.DeleteCredential(AppNamePrefix() + host);
        }

        /// <summary>
        /// Gets a credential from the cache.
        /// </summary>
        /// <param name="serviceUri">The endpoint to get the credential from.</param>
        /// <returns>A tuple of username + password.</returns>
        public static CachedCredential? ReadCredential(Uri serviceUri)
        {
            string host = serviceUri?.OriginalString ?? throw new ArgumentNullException(nameof(serviceUri));
            var credential = CredentialManager.ReadCredential(AppNamePrefix() + host);
            if (credential != null)
            {
                return new CachedCredential(credential.UserName, credential.Password);
            }

            return null;
        }

        /// <summary>
        /// Clears the credential cache.
        /// </summary>
        public static void ClearCredentials()
        {
            var prefix = AppNamePrefix();
            foreach (var item in CredentialManager.EnumerateCredentials())
            {
                if (item.ApplicationName.StartsWith(prefix))
                {
                    CredentialManager.DeleteCredential(item.ApplicationName);
                }
            }
        }

        private static class CredentialManager
        {
            private static string ConvertToUnsecureString(SecureString securePassword)
            {
                if (securePassword == null)
                {
                    throw new ArgumentNullException("securePassword");
                }

                IntPtr unmanagedString = IntPtr.Zero;
                try
                {
                    unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                    return Marshal.PtrToStringUni(unmanagedString) ?? string.Empty;
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }

            public static Credential? ReadCredential(string applicationName)
            {
                IntPtr nCredPtr;
                bool read = CredRead(applicationName, CredentialType.Generic, 0, out nCredPtr);
                if (read)
                {
                    using (CriticalCredentialHandle critCred = new CriticalCredentialHandle(nCredPtr))
                    {
                        return ReadCredential(critCred.GetCredential());
                    }
                }

                return null;
            }

            private static Credential ReadCredential(CREDENTIAL credential)
            {
                string applicationName = Marshal.PtrToStringUni(credential.TargetName) ?? string.Empty;
                string userName = Marshal.PtrToStringUni(credential.UserName) ?? string.Empty;
                string secret = string.Empty;
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2) ?? string.Empty;
                }

                return new Credential(credential.Type, applicationName, userName, secret);
            }

            public static int DeleteCredential(string applicationName)
            {
                bool deleted = CredDelete(applicationName, CredentialType.Generic, 0);
                int lastError = Marshal.GetLastWin32Error();
                if (deleted)
                {
                    return 0;
                }

                return lastError;
            }

            public static int WriteCredential(string applicationName, string userName, SecureString password)
            {
                var secret = ConvertToUnsecureString(password);
                byte[] byteArray = Encoding.Unicode.GetBytes(secret);
                if (byteArray.Length > 512)
                {
                    throw new ArgumentOutOfRangeException("secret", "The secret message has exceeded 512 bytes.");
                }

                CREDENTIAL credential = new CREDENTIAL
                {
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero,
                    Comment = IntPtr.Zero,
                    TargetAlias = IntPtr.Zero,
                    Type = CredentialType.Generic,
                    Persist = (uint)CredentialPersistence.Session,
                    CredentialBlobSize = (uint)Encoding.Unicode.GetBytes(secret).Length,
                    TargetName = Marshal.StringToCoTaskMemUni(applicationName),
                    CredentialBlob = Marshal.StringToCoTaskMemUni(secret),
                    UserName = Marshal.StringToCoTaskMemUni(userName ?? Environment.UserName),
                };

                bool written = CredWrite(ref credential, 0);
                int lastError = Marshal.GetLastWin32Error();

                Marshal.FreeCoTaskMem(credential.TargetName);
                Marshal.FreeCoTaskMem(credential.CredentialBlob);
                Marshal.FreeCoTaskMem(credential.UserName);

                if (written)
                {
                    return 0;
                }

                throw new Exception(string.Format("CredWrite failed with the error code {0}.", lastError));
            }

            public static IReadOnlyList<Credential> EnumerateCredentials()
            {
                List<Credential> result = new List<Credential>();

                int count;
                IntPtr pCredentials;
                bool ret = CredEnumerate(null, 0, out count, out pCredentials);
                if (ret)
                {
                    for (int n = 0; n < count; n++)
                    {
                        IntPtr credential = Marshal.ReadIntPtr(pCredentials, n * Marshal.SizeOf(typeof(IntPtr)));
                        result.Add(ReadCredential((CREDENTIAL)Marshal.PtrToStructure(credential, typeof(CREDENTIAL)) !));
                    }
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(lastError);
                }

                return result;
            }

            [DllImport("Advapi32.dll", EntryPoint = "CredDelete", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool CredDelete(string target, CredentialType type, int reservedFlag);

            [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPtr);

            [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool CredEnumerate(string? filter, int flag, out int count, out IntPtr pCredentials);

            [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
            private static extern bool CredFree([In] IntPtr cred);

            private enum CredentialPersistence : uint
            {
                Session = 1,
                LocalMachine,
                Enterprise,
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct CREDENTIAL
            {
                public uint Flags;
                public CredentialType Type;
                public IntPtr TargetName;
                public IntPtr Comment;
                public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
                public uint CredentialBlobSize;
                public IntPtr CredentialBlob;
                public uint Persist;
                public uint AttributeCount;
                public IntPtr Attributes;
                public IntPtr TargetAlias;
                public IntPtr UserName;
            }

            private sealed class CriticalCredentialHandle : Microsoft.Win32.SafeHandles.CriticalHandleZeroOrMinusOneIsInvalid
            {
                public CriticalCredentialHandle(IntPtr preexistingHandle)
                {
                    SetHandle(preexistingHandle);
                }

                public CREDENTIAL GetCredential()
                {
                    if (!IsInvalid)
                    {
                        CREDENTIAL credential = (CREDENTIAL)(Marshal.PtrToStructure(handle, typeof(CREDENTIAL)) !);
                        return credential;
                    }

                    throw new InvalidOperationException("Invalid CriticalHandle!");
                }

                protected override bool ReleaseHandle()
                {
                    if (!IsInvalid)
                    {
                        CredFree(handle);
                        SetHandleAsInvalid();
                        return true;
                    }

                    return false;
                }
            }

            public enum CredentialType
            {
                Generic = 1,
                DomainPassword,
                DomainCertificate,
                DomainVisiblePassword,
                GenericCertificate,
                DomainExtended,
                Maximum,
                MaximumEx = Maximum + 1000,
            }

            public sealed class Credential : IDisposable
            {
                private readonly string _applicationName;
                private readonly string _userName;
                private readonly SecureString _password;
                private readonly CredentialType _credentialType;
                private bool _disposedValue;

                public CredentialType CredentialType
                {
                    get { return _credentialType; }
                }

                public string ApplicationName
                {
                    get { return _applicationName; }
                }

                public string UserName
                {
                    get { return _userName; }
                }

                public SecureString Password
                {
                    get { return _password; }
                }

                public Credential(CredentialType credentialType, string applicationName, string userName, string password)
                {
                    _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
                    _userName = userName ?? throw new ArgumentNullException(nameof(userName));
                    _password = new SecureString();
                    if (password != null)
                    {
                        foreach (char c in password)
                        {
                            _password.AppendChar(c);
                        }
                    }

                    _credentialType = credentialType;
                }

                public override string ToString()
                {
                    return string.Format("CredentialType: {0}, ApplicationName: {1}, UserName: {2}, Password: {3}", CredentialType, ApplicationName, UserName, Password);
                }

                private void Dispose(bool disposing)
                {
                    if (!_disposedValue)
                    {
                        if (disposing)
                        {
                            _password.Dispose();
                        }

                        _disposedValue = true;
                    }
                }

                public void Dispose()
                {
                    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}
