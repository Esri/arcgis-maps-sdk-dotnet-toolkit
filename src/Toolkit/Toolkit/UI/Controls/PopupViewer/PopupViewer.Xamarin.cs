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

#if XAMARIN
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class PopupViewer
    {
        private PopupManager _popupManager;

        /// <summary>
        /// Gets or sets the associated PopupManager which contains popup and sketch editor.
        /// </summary>
        private PopupManager PopupManagerImpl
        {
            get => _popupManager;
            set
            {
                if (_popupManager != value)
                {
                    _popupManager = value;
                    Refresh();
                }
            }
        }
    }
}
#endif