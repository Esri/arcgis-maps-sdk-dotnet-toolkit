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

using Esri.ArcGISRuntime.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// The Compass Control showing the heading on the map when the rotation is not North up / 0.
/// </summary>
public class Compass : View, ICompass
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Compass"/> class.
    /// </summary>
    public Compass()
    {
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Start;
        WidthRequest = 30;
        HeightRequest = 30;
    }

    /// <summary>
    /// Identifies the <see cref="Heading"/> bindable property.
    /// </summary>
    public static readonly BindableProperty HeadingProperty =
        BindableProperty.Create(nameof(Heading), typeof(double), typeof(Compass), 0d, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the Heading for the compass.
    /// </summary>
    public double Heading
    {
        get { return (double)GetValue(HeadingProperty); }
        set { SetValue(HeadingProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="AutoHide"/> bindable property.
    /// </summary>
    public static readonly BindableProperty AutoHideProperty =
        BindableProperty.Create(nameof(AutoHide), typeof(bool), typeof(Compass), true, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0.
    /// </summary>
    public bool AutoHide
    {
        get { return (bool)GetValue(AutoHideProperty); }
        set { SetValue(AutoHideProperty, value); }
    }

    /// <summary>
    /// Gets or sets the GeoView property that can be attached to a Compass control to accurately set the heading, instead of
    /// setting the <see cref="Compass.Heading"/> property directly.
    /// </summary>
    public GeoView? GeoView
    {
        get { return GetValue(GeoViewProperty) as GeoView; }
        set { SetValue(GeoViewProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="GeoView"/> Dependency Property.
    /// </summary>
    public static readonly BindableProperty GeoViewProperty =
        BindableProperty.Create(nameof(Compass.GeoView), typeof(GeoView), typeof(Compass), null, BindingMode.OneWay, propertyChanged: OnGeoViewPropertyChanged);

    private static void OnGeoViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if WINDOWS || __IOS__ || __ANDROID__
        if(bindable is Compass c && c.PlatformGeoView != null)
        {
            c.PlatformGeoView.GeoView = (c.GeoView?.Handler as GeoViewHandler<Esri.ArcGISRuntime.Maui.IGeoView, Esri.ArcGISRuntime.UI.Controls.GeoView>)?.PlatformView;
        }
#endif
    }

#if WINDOWS || __IOS__ || __ANDROID__
    private Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass? PlatformGeoView => Handler?.PlatformView as Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass;
#endif
}
