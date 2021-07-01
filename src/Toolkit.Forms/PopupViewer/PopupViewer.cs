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
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The PopupViewer control is used to display details and media, edit attributes, geometry and related records,
    /// manage attachments of an <see cref="Data.ArcGISFeature"/> or a <see cref="ArcGISRuntime.UI.Graphic"/>
    /// as defined in its <see cref="Mapping.Popups.Popup"/>.
    /// </summary>
    public class PopupViewer : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopupViewer"/> class.
        /// </summary>
        public PopupViewer()
#if __ANDROID__
            : this(new UI.Controls.PopupViewer(global::Android.App.Application.Context))
#else
            : this(new UI.Controls.PopupViewer())
#endif
        {
        }

        internal PopupViewer(UI.Controls.PopupViewer nativePopupViewer)
        {
            NativePopupViewer = nativePopupViewer;

#if NETFX_CORE
            nativePopupViewer.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal UI.Controls.PopupViewer NativePopupViewer { get; }

        /// <summary>
        /// Identifies the <see cref="PopupManager"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PopupManagerProperty =
            BindableProperty.Create(nameof(PopupManager), typeof(PopupManager), typeof(PopupViewer), null, BindingMode.OneWay, null, OnPopupManagerProperty);

        /// <summary>
        /// Gets or sets the associated PopupManager which contains popup and sketch editor.
        /// </summary>
        public PopupManager? PopupManager
        {
            get { return (PopupManager)GetValue(PopupManagerProperty); }
            set { SetValue(PopupManagerProperty, value); }
        }

        private static void OnPopupManagerProperty(BindableObject bindable, object? oldValue, object? newValue)
        {
            var popupViewer = bindable as PopupViewer;
            if (popupViewer?.NativePopupViewer != null)
            {
                popupViewer.NativePopupViewer.PopupManager = newValue as PopupManager;
                popupViewer.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="Foreground"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ForegroundProperty =
            BindableProperty.Create(nameof(Foreground), typeof(Color), typeof(PopupViewer), Color.Black, BindingMode.OneWay, null, OnForegroundChanged);

        /// <summary>
        /// Gets or sets the foreground color.
        /// </summary>
        public Color Foreground
        {
            get { return (Color)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        private static void OnForegroundChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var nativeView = ((PopupViewer)bindable).NativePopupViewer;
            if (newValue != null)
            {
                nativeView.SetForeground(((Color)newValue).ToNativeColor());
            }
        }
    }
}
