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
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass;
#elif __IOS__
using NativeViewType = UIKit.UIView;
#elif __ANDROID__
using NativeViewType = Android.Views.View;
#else
using NativeViewType = System.Object;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Handlers
{
    public class CompassHandler : ViewHandler<ICompass, NativeViewType>
    {
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

#if WINDOWS || __IOS__ || __ANDROID__
        /// <inheritdoc />
        protected override void ConnectHandler(NativeViewType platformView)
        {
            base.ConnectHandler(platformView);
            if(platformView is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += PlatformView_PropertyChanged;
            }
            UpdateHeadingFromNativeCompass(VirtualView);
        }

        private void PlatformView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass.Heading))
            {
                UpdateHeadingFromNativeCompass(VirtualView);
            }
        }

        private void UpdateHeadingFromNativeCompass(ICompass? compass)
        {
            if (PlatformView is Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass view && compass is not null)
            {
                _isUpdatingHeadingFromGeoView = true;
                compass.Heading = view.Heading;
                _isUpdatingHeadingFromGeoView = false;
            }
        }

        /// <inheritdoc />
        protected override void DisconnectHandler(NativeViewType platformView)
        {
            if (platformView is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= PlatformView_PropertyChanged;
            }
            base.DisconnectHandler(platformView);
        }

        /// <inheritdoc />
        public override void PlatformArrange(Rect rect)
        {
            base.PlatformArrange(rect);
#if WINDOWS
            PlatformView.Width = Math.Max(0, rect.Width - 1);
            PlatformView.Height = rect.Height;
#elif __ANDROID__
            var lp = PlatformView.LayoutParameters;
            if (lp != null && Context != null)
            {
                var scale = (VirtualView as View)?.Window?.DisplayDensity ?? 1f;
                lp.Width = (int)(rect.Width * scale);
                lp.Height = (int)(rect.Height = scale);
            }
            PlatformView.LayoutParameters = lp;
#endif
        }
#endif

        private static void MapAutoHide(CompassHandler handler, ICompass compass)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).AutoHide = compass.AutoHide;
#endif
        }

        private static void MapGeoView(CompassHandler handler, ICompass compass)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (compass.GeoView?.Handler is MapViewHandler mvh)
                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).GeoView = mvh.PlatformView;
            else if (compass.GeoView?.Handler is SceneViewHandler svh)
                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).GeoView = svh.PlatformView;
            else
                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).GeoView = null;
#endif
        }

        private static void MapHeading(CompassHandler handler, ICompass compass)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).Heading = compass.Heading;
#endif
        }

        private bool _isUpdatingHeadingFromGeoView;

        /// <inheritdoc />
#if WINDOWS || __IOS__
        protected override NativeViewType CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass();
#elif __ANDROID__
        protected override NativeViewType CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass(this.Context);
#else
        protected override object CreatePlatformView()
        {
            throw new NotImplementedException();
        }
#endif
    }
}
