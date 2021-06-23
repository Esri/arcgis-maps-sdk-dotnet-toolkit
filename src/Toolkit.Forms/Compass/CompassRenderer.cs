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
#if !NETSTANDARD2_0
using Xamarin.Forms;
#if __ANDROID__
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using Xamarin.Forms.Platform.iOS;
#elif NETFX_CORE
using Xamarin.Forms.Platform.UWP;
#endif

using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Compass), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.CompassRenderer))]

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class CompassRenderer : ViewRenderer<Compass, Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass?>
    {
#if __ANDROID__
        public CompassRenderer(Android.Content.Context context)
            : base(context)
        {
        }
#endif

        protected override void OnElementChanged(ElementChangedEventArgs<Compass> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement != null)
            {
                e.OldElement.SizeChanged -= Element_SizeChanged;
            }

            if (e.NewElement != null)
            {
                if (Control == null)
                {
#if __ANDROID__
                    UI.Controls.Compass ctrl = new UI.Controls.Compass(Context);
#else
                    UI.Controls.Compass ctrl = new UI.Controls.Compass();
#endif
                    ctrl.GeoView = Element.GeoView?.GetNativeGeoView();
                    ctrl.Heading = Element.Heading;
                    ctrl.AutoHide = Element.AutoHide;

#if NETFX_CORE
                    ctrl.Margin = new Windows.UI.Xaml.Thickness(0, 0, 1, 0);
                    ctrl.RegisterPropertyChangedCallback(UI.Controls.Compass.HeadingProperty, (d, args) => UpdateHeadingFromNativeCompass());
#elif !NETSTANDARD2_0
                    ctrl.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(UI.Controls.Compass.Heading))
                        {
                            UpdateHeadingFromNativeCompass();
                        }
                    };
#endif
                    SetNativeControl(ctrl);
                    UpdateHeadingFromNativeCompass();
                }

                e.NewElement.SizeChanged += Element_SizeChanged;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Element != null)
            {
                Element.SizeChanged -= Element_SizeChanged;
            }

            base.Dispose(disposing);
        }

        private void Element_SizeChanged(object sender, EventArgs e)
        {
            if (Control != null)
            {
#if NETFX_CORE
                Control.Width = Math.Max(0, Element.Width - 1);
                Control.Height = Element.Height;
#elif __ANDROID__
                var lp = Control.LayoutParameters;
                if (lp != null && Context != null)
                {
                    lp.Width = (int)Context.ToPixels(Element.Width);
                    lp.Height = (int)Context.ToPixels(Element.Height);
                }

                Control.LayoutParameters = lp;
#endif
            }
        }

        private bool _isUpdatingHeadingFromGeoView;

        private void UpdateHeadingFromNativeCompass()
        {
            _isUpdatingHeadingFromGeoView = true;
            if (Element != null)
            {
                Element.Heading = Control?.Heading ?? 0d;
            }

            _isUpdatingHeadingFromGeoView = false;
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Control != null)
            {
                if (e.PropertyName == Compass.GeoViewProperty.PropertyName)
                {
                    Control.GeoView = Element.GeoView?.GetNativeGeoView();
                }
                else if (e.PropertyName == Compass.HeadingProperty.PropertyName)
                {
                    if (!_isUpdatingHeadingFromGeoView)
                    {
                        Control.Heading = Element.Heading;
                    }
                }
                else if (e.PropertyName == Compass.AutoHideProperty.PropertyName)
                {
                    Control.AutoHide = Element.AutoHide;
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }
    }
}
#endif
