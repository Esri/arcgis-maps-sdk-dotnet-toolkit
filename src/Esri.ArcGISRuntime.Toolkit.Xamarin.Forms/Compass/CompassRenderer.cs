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
    internal class CompassRenderer : ViewRenderer<Compass, Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass>
    {
#if __IOS__
        private UIKit.NSLayoutConstraint _widthConstraint;
        private UIKit.NSLayoutConstraint _heightConstraint;
#endif

#if __ANDROID__
        public CompassRenderer(Android.Content.Context context)
            : base(context)
        {
        }
#endif

        protected override void OnElementChanged(ElementChangedEventArgs<Compass> e)
        {
            base.OnElementChanged(e);

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
                    //ctrl.SizeChanged += (o, args) => InvalidateMeasure();
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
            }
        }

        private bool _isUpdatingHeadingFromGeoView;

        private void UpdateHeadingFromNativeCompass()
        {
            _isUpdatingHeadingFromGeoView = true;
            if (Element != null)
            {
                Element.Heading = Control.Heading;
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
#if __IOS__
                else if (e.PropertyName == VisualElement.WidthProperty.PropertyName)
                {
                    if (_widthConstraint != null)
                    {
                        _widthConstraint.Active = false;
                    }

                    if (Element.Width >= 0)
                    {
                        _widthConstraint = Control.WidthAnchor.ConstraintEqualTo((nfloat)Element.Width);
                        if (_widthConstraint != null)
                        {
                            _widthConstraint.Active = true;
                        }
                    }
                }
                else if (e.PropertyName == VisualElement.HeightProperty.PropertyName)
                {
                    if (_heightConstraint != null)
                    {
                        _heightConstraint.Active = false;
                    }

                    if (Element.Height >= 0)
                    {
                        _heightConstraint = Control.HeightAnchor.ConstraintEqualTo((nfloat)Element.Height);
                        if (_heightConstraint != null)
                        {
                            _heightConstraint.Active = true;
                        }
                    }
                }
#endif
            }

            base.OnElementPropertyChanged(sender, e);
        }
    }
}
#endif