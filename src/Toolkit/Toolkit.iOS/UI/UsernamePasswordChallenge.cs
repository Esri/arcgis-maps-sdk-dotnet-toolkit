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
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Security;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    internal class UsernamePasswordChallenge
    {
        private readonly CredentialRequestInfo _info;
        private readonly UIAlertController _alertController;
        private UITextField? _usernameTextField;
        private UITextField? _passwordTextField;
        private UIAlertAction _signInAction;
        private TaskCompletionSource<NetworkCredential?>? _tcs;

        public UsernamePasswordChallenge(CredentialRequestInfo info)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));

            var host = _info.ProxyServiceUri?.Host ?? _info.ServiceUri?.Host;
            var message = string.IsNullOrEmpty(host) ? "You need to sign in." : $"You need to sign in to access '{host}'.";
            _alertController = UIAlertController.Create("Credentials Required", message, UIAlertControllerStyle.Alert);

            _signInAction = UIAlertAction.Create("Sign In", UIAlertActionStyle.Default, OnAccept);
            _signInAction.Enabled = false;
            var cancelAction = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, OnCancel);

            _alertController.AddAction(_signInAction);
            _alertController.AddAction(cancelAction);
            _alertController.AddTextField((textField) =>
            {
                textField.Placeholder = "Username";
                textField.ReturnKeyType = UIReturnKeyType.Next;
                textField.AddTarget(OnTextFieldChanged, UIControlEvent.AllEditingEvents);
                _usernameTextField = textField;
            });
            _alertController.AddTextField((textField) =>
            {
                textField.Placeholder = "Password";
                textField.EnablesReturnKeyAutomatically = true;
                textField.SecureTextEntry = true;
                textField.AddTarget(OnTextFieldChanged, UIControlEvent.AllEditingEvents);
                _passwordTextField = textField;
            });
            _alertController.PreferredAction = _signInAction;
        }

        public Task<NetworkCredential?> ShowAsync()
        {
            _tcs = new TaskCompletionSource<NetworkCredential?>();
            UIViewControllerHelper.PresentViewControllerOnTop(_alertController);
            return _tcs.Task;
        }

        // Ensures that the "Sign in" button is disabled unless both username and password are set
        private void OnTextFieldChanged(object sender, EventArgs e)
        {
            var username = _usernameTextField?.Text;
            var password = _passwordTextField?.Text;
            _signInAction.Enabled = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
        }

        private void OnAccept(UIAlertAction a)
        {
            var username = _usernameTextField?.Text;
            var password = _passwordTextField?.Text;
            _alertController.DismissViewController(true, () =>
            {
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    _tcs?.TrySetResult(new NetworkCredential(username, password));
                }
                else
                {
                    _tcs?.TrySetResult(null);
                }
            });
        }

        private void OnCancel(UIAlertAction a)
        {
            _alertController.DismissViewController(true, () => _tcs?.SetResult(null));
        }
    }
}