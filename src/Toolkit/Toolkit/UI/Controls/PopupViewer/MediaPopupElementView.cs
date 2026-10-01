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
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    public partial class MediaPopupElementView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPopupElementView"/> class.
        /// </summary>
        public MediaPopupElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(MediaPopupElementView);
#endif
#if WPF || WINDOWS_XAML
            PreviousMediaButtonText = Properties.Resources.GetString("PopupViewerPreviousMediaButtonText");
            NextMediaButtonText = Properties.Resources.GetString("PopupViewerNextMediaButtonText");
#endif
        }

#if WPF || WINDOWS_XAML
        /// <summary>
        /// Gets or sets the accessible name and tooltip text used for the button that shows the previous media item.
        /// </summary>
        public string? PreviousMediaButtonText
        {
            get { return GetValue(PreviousMediaButtonTextProperty) as string; }
            set { SetValue(PreviousMediaButtonTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PreviousMediaButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviousMediaButtonTextProperty =
            PropertyHelper.CreateProperty<string, MediaPopupElementView>(nameof(PreviousMediaButtonText));

        /// <summary>
        /// Gets or sets the accessible name and tooltip text used for the button that shows the next media item.
        /// </summary>
        public string? NextMediaButtonText
        {
            get { return GetValue(NextMediaButtonTextProperty) as string; }
            set { SetValue(NextMediaButtonTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="NextMediaButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NextMediaButtonTextProperty =
            PropertyHelper.CreateProperty<string, MediaPopupElementView>(nameof(NextMediaButtonText));
#endif

        /// <summary>
        /// Gets or sets the MediaPopupElement.
        /// </summary>
        public MediaPopupElement? Element
        {
            get { return GetValue(ElementProperty) as MediaPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            PropertyHelper.CreateProperty<MediaPopupElement, MediaPopupElementView>(nameof(Element), null, (s, oldValue, newValue) => s.OnElementPropertyChanged());
    }
}