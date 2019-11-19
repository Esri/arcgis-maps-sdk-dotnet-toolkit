// /*******************************************************************************
//  * Copyright 2012-2019 Esri
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

#if !NETSTANDARD2_0
using System.ComponentModel;
using Xamarin.Forms;
#if __ANDROID__
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using Xamarin.Forms.Platform.iOS;
#elif NETFX_CORE
using Windows.UI.Xaml;
using Xamarin.Forms.Platform.UWP;
#endif

using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Bookmarks), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.BookmarksRenderer))]
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class BookmarksRenderer : ViewRenderer<Bookmarks, UI.Controls.Bookmarks>
    {
#if __ANDROID__
        public BookmarksRenderer(Android.Content.Context context)
            : base(context)
        {
        }
#endif

        protected override void OnElementChanged(ElementChangedEventArgs<Bookmarks> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (Control == null)
                {
#if __ANDROID__
                    UI.Controls.Bookmarks ctrl = new UI.Controls.Bookmarks(Context);
#else
                    UI.Controls.Bookmarks ctrl = new UI.Controls.Bookmarks();
#endif
                    ctrl.GeoView = Element.GeoView?.GetNativeGeoView();

                    SetNativeControl(ctrl);
#if NETFX_CORE
                    ctrl.SizeChanged += OnNativeSizeChanged;
#endif

#if __IOS__
                    SetNeedsDisplay();
#elif __ANDROID__
                    Invalidate();
#elif NETFX_CORE
                    InvalidateArrange();
#endif

                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Control != null)
            {
                if (e.PropertyName == Bookmarks.GeoViewProperty.PropertyName)
                {
                    Control.GeoView = Element.GeoView?.GetNativeGeoView();
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
#if NETFX_CORE
            if (Control != null)
            {
                Control.SizeChanged -= OnNativeSizeChanged;
            }
#endif
        }

#if NETFX_CORE
        private void OnNativeSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Element.Layout(new Rectangle(0, 0, e.NewSize.Width, e.NewSize.Height));
        }

        /// <inheritdoc />
        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            Control.Measure(availableSize);
            return Control.DesiredSize;
        }
#endif
    }
}
#endif