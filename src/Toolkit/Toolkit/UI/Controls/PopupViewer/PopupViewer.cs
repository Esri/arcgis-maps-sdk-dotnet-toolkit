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

using Esri.ArcGISRuntime.Mapping.Popups;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The PopupViewer control is used to display details and media, edit attributes, geometry and related records,
    /// manage attachments of an <see cref="Data.ArcGISFeature"/> or a <see cref="ArcGISRuntime.UI.Graphic"/>
    /// as defined in its <see cref="Mapping.Popups.Popup"/>.
    /// </summary>
    public partial class PopupViewer : Control
    {
#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="PopupViewer"/> class.
        /// </summary>
        public PopupViewer()
            : base()
        {
            Initialize();
        }
#endif

        /// <summary>
        /// Gets or sets the associated PopupManager which contains popup and sketch editor.
        /// </summary>
        public PopupManager PopupManager
        {
            get => PopupManagerImpl;
            set => PopupManagerImpl = value;
        }
    }
}