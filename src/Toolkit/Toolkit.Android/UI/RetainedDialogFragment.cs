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

using Android.App;
using Android.OS;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// A dialog that handles device orientation and rotation changes.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    internal class RetainedDialogFragment : DialogFragment
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public override void OnActivityCreated(Bundle? savedInstanceState)
        {
            RetainInstance = true;
            base.OnActivityCreated(savedInstanceState);
        }

        public override void OnDestroyView()
        {
            // workaround for https://code.google.com/p/android/issues/detail?id=17423
            if (Dialog != null && RetainInstance)
            {
                Dialog.SetDismissMessage(null);
            }

            base.OnDestroyView();
        }
    }
}