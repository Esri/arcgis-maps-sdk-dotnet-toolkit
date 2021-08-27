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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Esri.ArcGISRuntime.Security;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    internal class CredentialDialog
    {
        private const int MaxUserNameLength = 256 + 1 + 256;
        private const int MaxPasswordLength = 256;
        private const uint CredUISuccess = 0; // ERROR_SUCCESS flag
        private const uint CredUICanceled = 1223; // ERROR_CANCELED flag
        private const uint CredUIWinGeneric = 0x1; // CREDUIWIN_GENERIC flag

        private readonly CredentialRequestInfo _info;

        public CredentialDialog(CredentialRequestInfo info)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
        }

        // Shows the dialog in the current active window.
        public bool ShowDialog() => ShowDialog(null);

        // Shows the dialog in the specified window.
        public bool ShowDialog(Window? owner)
        {
            var handle = owner is null
                ? GetActiveWindow()
                : new WindowInteropHelper(owner).Handle;
            return ShowDialog(handle);
        }

        private bool ShowDialog(IntPtr handle)
        {
            // Prepare CREDUI_INFO struct
            var caption = "Credentials Required";
            var host = _info.ProxyServiceUri?.Host ?? _info.ServiceUri?.Host;
            var message = string.IsNullOrEmpty(host) ? "You need to sign in." : $"You need to sign in to access '{host}'."; ;
            var info = new CredUIInfo
            {
                Size = Marshal.SizeOf<CredUIInfo>(),
                Parent = handle,
                CaptionText = caption,
                MessageText = message,
            };

            var save = false;
            var outBuffer = IntPtr.Zero;
            var outBufferSize = 0u;
            var package = 0u;
            try
            {
                // Show generic CredUI prompt
                var result = CredUIPromptForWindowsCredentials(
                    ref info,
                    0,
                    ref package,
                    IntPtr.Zero,
                    0,
                    out outBuffer,
                    out outBufferSize,
                    ref save,
                    CredUIWinGeneric);

                if (result == CredUISuccess)
                {
                    // Unpack entered username/password
                    var username = new StringBuilder(MaxUserNameLength);
                    var password = new StringBuilder(MaxPasswordLength);
                    var usernameSize = (uint)MaxUserNameLength;
                    var passwordSize = (uint)MaxPasswordLength;
                    var domainSize = 0u;
                    var unpack = CredUnPackAuthenticationBuffer(
                        0,
                        outBuffer,
                        outBufferSize,
                        username,
                        ref usernameSize,
                        null,
                        ref domainSize,
                        password,
                        ref passwordSize);
                    if (!unpack)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    UserName = username.ToString();
                    Password = password.ToString();
                }
                else if (result == CredUICanceled)
                {
                    return false;
                }
                else
                {
                    throw new Win32Exception((int)result);
                }
            }
            finally
            {
                if (outBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(outBuffer);
                }
            }

            return false;
        }

        #region Interop

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetActiveWindow();

        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        private static extern uint CredUIPromptForWindowsCredentials(
            ref CredUIInfo pUiInfo,
            uint dwAuthError,
            ref uint pulAuthPackage,
            IntPtr pvInAuthBuffer,
            uint ulInAuthBufferSize,
            out IntPtr ppvOutAuthBuffer,
            out uint pulOutAuthBufferSize,
            [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
            uint dwFlags);

        [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredUnPackAuthenticationBuffer(
            uint dwFlags,
            IntPtr pAuthBuffer,
            uint cbAuthBuffer,
            StringBuilder pszUserName,
            ref uint pcchMaxUserName,
            StringBuilder pszDomainName,
            ref uint pcchMaxDomainName,
            StringBuilder pszPassword,
            ref uint pcchMaxPassword);

        private struct CredUIInfo
        {
            public int Size;
            public IntPtr Parent;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string MessageText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string CaptionText;
            public IntPtr Banner;
        }

        #endregion

        // Gets the entered username.
        public string UserName { get; private set; } = string.Empty;

        // Gets the entered password.
        public string Password { get; private set; } = string.Empty;
    }
}