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
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    internal class UsernamePasswordChallenge
    {
        public const string DialogTag = "arcgis_user_auth_dialog";

        private class UsernamePasswordDialog : RetainedDialogFragment
        {
            private readonly CredentialRequestInfo _info;
            private readonly Action _onPositive;
            private readonly Action _onNegative;
            private EditText? _usernameEditText;
            private EditText? _passwordEditText;

            public UsernamePasswordDialog(CredentialRequestInfo info, Action onPositive, Action onNegative)
            {
                _info = info;
                _onPositive = onPositive;
                _onNegative = onNegative;
            }

            public override Dialog? OnCreateDialog(Bundle? savedInstanceState)
            {
                // Build the dialog layout
                var builder = new AlertDialog.Builder(Activity!);
                var inflater = Activity!.LayoutInflater!;
                var view = inflater.Inflate(Resource.Layout.UsernamePasswordDialog, null);

                var titleTextView = view!.FindViewById<TextView>(Resource.Id.AuthTitle);
                var hostnameTextView = view.FindViewById<TextView>(Resource.Id.AuthHostName);
                _usernameEditText = view.FindViewById<EditText>(Resource.Id.AuthUserName);
                _passwordEditText = view.FindViewById<EditText>(Resource.Id.AuthPassword);

                // Apply text to layout
                titleTextView!.Text = "Authentication Required";
                hostnameTextView!.Text = _info.ProxyServiceUri?.Host ?? _info.ServiceUri!.Host;
                _usernameEditText!.Hint = "Username";
                _passwordEditText!.Hint = "Password";

                // Configure layout
                builder.SetPositiveButton("Sign in", OnPositiveButton);
                builder.SetNegativeButton("Cancel", OnNegativeButton);
                builder.SetView(view);

                return builder.Create();
            }

            public override void OnCancel(IDialogInterface? dialog)
            {
                _onNegative();
            }

            private void OnPositiveButton(object sender, DialogClickEventArgs e)
            {
                _onPositive();
            }

            private void OnNegativeButton(object sender, DialogClickEventArgs e)
            {
                _onNegative();
            }

            public string? UserName => _usernameEditText?.Text;

            public string? Password => _passwordEditText?.Text;
        }

        private readonly Activity _context;
        private UsernamePasswordDialog _dialog;
        private TaskCompletionSource<NetworkCredential?>? _tcs;

        public UsernamePasswordChallenge(Activity context, CredentialRequestInfo info)
        {
            _context = context;
            _dialog = new UsernamePasswordDialog(info, OnPositive, OnNegative);
        }

        private void OnPositive()
        {
            _tcs!.TrySetResult(new NetworkCredential(_dialog.UserName, _dialog.Password));
        }

        private void OnNegative()
        {
            _tcs!.TrySetResult(null);
        }

        public Task<NetworkCredential?> ShowAsync()
        {
            _tcs = new TaskCompletionSource<NetworkCredential?>();
            Dispatcher.RunAsyncAction(() =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                _dialog.Show(_context.FragmentManager!, DialogTag);
#pragma warning restore CS0618 // Type or member is obsolete
            });
            return _tcs.Task;
        }
    }
}
