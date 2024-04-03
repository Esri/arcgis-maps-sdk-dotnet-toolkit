// /*******************************************************************************
//  * Copyright 2012-2022 Esri
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
using Microsoft.Maui.Handlers;
using System.ComponentModel;
#if WINDOWS
using NativeViewType = Microsoft.UI.Xaml.FrameworkElement;
#elif __IOS__
using NativeViewType = UIKit.UIView;
#elif __ANDROID__
using NativeViewType = Android.Views.View;
#else
using NativeViewType = System.Object;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Handlers;

/// <summary>
/// Handler maps the MAUI compass control to underlying native implementation.
/// </summary>
[Obsolete("No longer in use")]
public class CompassHandler : ViewHandler<ICompass, NativeViewType>
{
    /// <summary>
    /// Maps properties from the Compass public API to handler implementation.
    /// </summary>
    public static PropertyMapper<ICompass, CompassHandler> CompassMapper = new PropertyMapper<ICompass, CompassHandler>(ViewHandler.ViewMapper)
    {
        [nameof(ICompass.AutoHide)] = MapAutoHide,
        [nameof(ICompass.GeoView)] = MapGeoView,
        [nameof(ICompass.Heading)] = MapHeading,
    };

    /// <summary>
    /// Instantiates a new instance of the <see cref="CompassHandler"/> class.
    /// </summary>
    public CompassHandler() : this(CompassMapper)
    {
    }


    /// <summary>
    /// Instantiates a new instance of the <see cref="CompassHandler"/> class.
    /// </summary>
    /// <param name="mapper">property mapper</param>
    /// <param name="commandMapper"></param>
    public CompassHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper ?? CompassMapper, commandMapper )
    {
    }

    private static void MapAutoHide(CompassHandler handler, ICompass compass)
    {
    }

    private static void MapGeoView(CompassHandler handler, ICompass compass)
    {
    }

    private static void MapHeading(CompassHandler handler, ICompass compass)
    {
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException"></exception>>
    protected override NativeViewType CreatePlatformView()
    {
        throw new NotSupportedException();
    }
}
#pragma warning restore CS1591