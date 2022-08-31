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

#if __IOS__ || __ANDROID__

using System.ComponentModel;
using Esri.ArcGISRuntime.UI.Controls;
using GeoView = Esri.ArcGISRuntime.UI.Controls.GeoView;
using MapView = Esri.ArcGISRuntime.UI.Controls.MapView;
using SceneView = Esri.ArcGISRuntime.UI.Controls.SceneView;
#if __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// The Compass Control showing the heading on the map when the rotation is not North up / 0.
/// </summary>
internal partial class Compass : Control, System.ComponentModel.INotifyPropertyChanged
{
    private const double DefaultSize = 30;
    private bool _headingSetByGeoView;

#if !__ANDROID__
    /// <summary>
    /// Initializes a new instance of the <see cref="Compass"/> class.
    /// </summary>
    public Compass()
        : base()
    {
        Initialize();
    }
#endif

    private double _heading;

    /// <summary>
    /// Gets or sets the Heading for the compass.
    /// </summary>
    /// <remarks>
    /// This property is read-only if the <see cref="GeoView"/> property is assigned.
    /// </remarks>
    public double Heading
    {
        get => _heading;
        set
        {
            if (_heading != value)
            {
                if (GeoView != null && !_headingSetByGeoView)
                {
                    throw new InvalidOperationException("The Heading Property is read-only when the GeoView property has been assigned");
                }

                _heading = value;
                UpdateCompassRotation(true);
                OnPropertyChanged(nameof(Heading));
            }
        }
    }
    private bool _autoHide = true;

    /// <summary>
    /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0.
    /// </summary>
    public bool AutoHide
    {
          get => _autoHide;
        set
        {
            _autoHide = value;
            SetVisibility(!(value && _heading == 0));
            OnPropertyChanged(nameof(AutoHide));
        }
    }



    private GeoView? _geoView;

    /// <summary>
    /// Gets or sets the GeoView for which the heading is displayed. This will accurately reflect the heading at the center of the GeoView.
    /// </summary>
    /// <seealso cref="Heading"/>
    public GeoView? GeoView
    {
        get => _geoView;
        set
        {
            var oldView = _geoView;
            _geoView = value;
            WireGeoViewPropertyChanged(oldView, _geoView);
            OnPropertyChanged(nameof(GeoView));
        }
    }

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void WireGeoViewPropertyChanged(GeoView? oldGeoView, GeoView? newGeoView)
    {
        var inpc = oldGeoView as INotifyPropertyChanged;
        if (inpc != null)
        {
            inpc.PropertyChanged -= GeoView_PropertyChanged;
        }

        inpc = newGeoView as INotifyPropertyChanged;
        if (inpc != null)
        {
            inpc.PropertyChanged += GeoView_PropertyChanged;
        }

        UpdateCompassFromGeoView(newGeoView);
    }

    private void GeoView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var view = GeoView;
        if ((view is MapView && e.PropertyName == nameof(MapView.MapRotation)) ||
            (view is SceneView && e.PropertyName == nameof(SceneView.Camera)))
        {
            UpdateCompassFromGeoView(GeoView);
        }
    }

    private void UpdateCompassFromGeoView(GeoView? view)
    {
        _headingSetByGeoView = true;
        Heading = (view is MapView mv) ? mv.MapRotation : (view is SceneView sv ? sv.Camera.Heading : 0);
        _headingSetByGeoView = false;
    }

    private void ResetRotation()
    {
        var view = GeoView;
        if (view is MapView)
        {
            ((MapView)view).SetViewpointRotationAsync(0);
        }
        else if (view is SceneView)
        {
            var sv = (SceneView)view;
            var c = sv.Camera;
            sv.SetViewpointCameraAsync(c.RotateTo(0, c.Pitch, c.Roll));
        }
    }
}
#endif