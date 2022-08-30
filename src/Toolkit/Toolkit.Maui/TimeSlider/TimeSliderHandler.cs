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
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;
#elif __IOS__
using NativeViewType = UIKit.UIView;
#elif __ANDROID__
using NativeViewType = Android.Views.View;
#else
using NativeViewType = System.Object;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Handlers
{
    public class TimeSliderHandler : ViewHandler<ITimeSlider, NativeViewType>
    {
        public static PropertyMapper<ITimeSlider, TimeSliderHandler> TimeSliderMapper = new PropertyMapper<ITimeSlider, TimeSliderHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ITimeSlider.CurrentExtent)] = MapCurrentExtent,
            [nameof(ITimeSlider.FullExtent)] = MapFullExtent,
            [nameof(ITimeSlider.TimeStepInterval)] = MapTimeStepInterval,
            [nameof(ITimeSlider.TimeSteps)] = MapTimeSteps,
            [nameof(ITimeSlider.PlaybackInterval)] = MapPlaybackInterval,
            [nameof(ITimeSlider.PlaybackDirection)] = MapPlaybackDirection,
            [nameof(ITimeSlider.PlaybackLoopMode)] = MapPlaybackLoopMode,
            [nameof(ITimeSlider.IsStartTimePinned)] = MapIsStartTimePinned,
            [nameof(ITimeSlider.IsEndTimePinned)] = MapIsEndTimePinned,
            [nameof(ITimeSlider.IsPlaying)] = MapIsPlaying,
            [nameof(ITimeSlider.FullExtentLabelFormat)] = MapFullExtentLabelFormat,
            [nameof(ITimeSlider.CurrentExtentLabelFormat)] = MapCurrentExtentLabelFormat,
            [nameof(ITimeSlider.TimeStepIntervalLabelFormat)] = MapTimeStepIntervalLabelFormat,
            [nameof(ITimeSlider.LabelMode)] = MapLabelMode,
            [nameof(ITimeSlider.ThumbStroke)] = MapThumbStroke,
            [nameof(ITimeSlider.ThumbFill)] = MapThumbFill,
            [nameof(ITimeSlider.CurrentExtentFill)] = MapCurrentExtentFill,
            [nameof(ITimeSlider.FullExtentFill)] = MapFullExtentFill,
            [nameof(ITimeSlider.FullExtentStroke)] = MapFullExtentStroke,
            [nameof(ITimeSlider.TimeStepIntervalTickFill)] = MapTimeStepIntervalTickFill,
            [nameof(ITimeSlider.PlaybackButtonsFill)] = MapPlaybackButtonsFill,
            [nameof(ITimeSlider.PlaybackButtonsStroke)] = MapPlaybackButtonsStroke,
            [nameof(ITimeSlider.FullExtentLabelColor)] = MapFullExtentLabelColor,
            [nameof(ITimeSlider.CurrentExtentLabelColor)] = MapCurrentExtentLabelColor,
            [nameof(ITimeSlider.TimeStepIntervalLabelColor)] = MapTimeStepIntervalLabelColor,

        };

        /// <summary>
        /// Instantiates a new instance of the <see cref="TimeSliderHandler"/> class.
        /// </summary>
        public TimeSliderHandler() : this(TimeSliderMapper)
        {
        }


        /// <summary>
        /// Instantiates a new instance of the <see cref="TimeSliderHandler"/> class.
        /// </summary>
        /// <param name="mapper">property mapper</param>
        /// <param name="commandMapper"></param>
        public TimeSliderHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper ?? TimeSliderMapper, commandMapper )
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
            //UpdateHeadingFromNativeTimeSlider(VirtualView);
            // TODO = fill for all properties
        }

        private void PlatformView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _isUpdatingHeadingFromGeoView = true;
            var view = PlatformView as TimeSlider;
            var thisTS = VirtualView as ITimeSlider;
            
            if (view == null || thisTS == null)
            {
                return;
            }

            switch(e.PropertyName)
            {
                case nameof(ITimeSlider.CurrentExtent):
                    thisTS.CurrentExtent = view.CurrentExtent;
                    break;
                case nameof(ITimeSlider.FullExtent):
                    thisTS.FullExtent = view.FullExtent;
                    break;
                case nameof(ITimeSlider.TimeStepInterval):
                    thisTS.TimeStepInterval = view.TimeStepInterval;
                    break;
                case nameof(ITimeSlider.TimeSteps):
                    thisTS.TimeSteps = view.TimeSteps;
                    break;
                case nameof(ITimeSlider.PlaybackInterval):
                    thisTS.PlaybackInterval = view.PlaybackInterval;
                    break;
                case nameof(ITimeSlider.PlaybackDirection):
                    thisTS.PlaybackDirection = view.PlaybackDirection;
                    break;
                case nameof(ITimeSlider.PlaybackLoopMode):
                    thisTS.PlaybackLoopMode = view.PlaybackLoopMode;
                    break;
                case nameof(ITimeSlider.IsStartTimePinned):
                    thisTS.IsStartTimePinned = view.IsStartTimePinned;
                    break;
                case nameof(ITimeSlider.IsEndTimePinned):
                    thisTS.IsEndTimePinned = view.IsEndTimePinned;
                    break;
                case nameof(ITimeSlider.IsPlaying):
                    thisTS.IsPlaying = view.IsPlaying;
                    break;
                case nameof(ITimeSlider.FullExtentLabelFormat):
                    thisTS.FullExtentLabelFormat = view.FullExtentLabelFormat;
                    break;
                case nameof(ITimeSlider.CurrentExtentLabelFormat):
                    thisTS.CurrentExtentLabelFormat = view.CurrentExtentLabelFormat;
                    break;
                case nameof(ITimeSlider.TimeStepIntervalLabelFormat):
                    thisTS.TimeStepIntervalLabelFormat = view.TimeStepIntervalLabelFormat;
                    break;
                case nameof(ITimeSlider.LabelMode):
                    thisTS.LabelMode = view.LabelMode;
                    break;
                case nameof(ITimeSlider.ThumbStroke):
                    thisTS.ThumbStroke = view.ThumbStroke;
                    break;
                case nameof(ITimeSlider.ThumbFill):
                    thisTS.ThumbFill = view.ThumbFill;
                    break;
                case nameof(ITimeSlider.CurrentExtentFill):
                    thisTS.CurrentExtentFill = view.CurrentExtentFill;
                    break;
                case nameof(ITimeSlider.FullExtentFill):
                    thisTS.FullExtentFill = view.FullExtentFill;
                    break;
                case nameof(ITimeSlider.FullExtentStroke):
                    thisTS.FullExtentStroke = view.FullExtentStroke;
                    break;
                case nameof(ITimeSlider.TimeStepIntervalTickFill):
                    thisTS.TimeStepIntervalTickFill = view.TimeStepIntervalTickFill;
                    break;
                case nameof(ITimeSlider.PlaybackButtonsFill):
                    thisTS.PlaybackButtonsFill = view.PlaybackButtonsFill;
                    break;
                case nameof(ITimeSlider.PlaybackButtonsStroke):
                    thisTS.PlaybackButtonsStroke = view.PlaybackButtonsStroke;
                    break;
                case nameof(ITimeSlider.FullExtentLabelColor):
                    thisTS.FullExtentLabelColor = view.FullExtentLabelColor;
                    break;
                case nameof(ITimeSlider.CurrentExtentLabelColor):
                    thisTS.CurrentExtentLabelColor = view.CurrentExtentLabelColor;
                    break;
                case nameof(ITimeSlider.TimeStepIntervalLabelColor):
                    thisTS.TimeStepIntervalLabelColor = view.TimeStepIntervalLabelColor;
                    break;
            }
            _isUpdatingHeadingFromGeoView = false;
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
#if  __ANDROID__
            var lp = PlatformView.LayoutParameters;
            if (lp != null && Context != null)
            {
                var scale = (VirtualView as View)?.Window?.DisplayDensity ?? 1f;
                lp.Width = (int)(rect.Width * scale);
                lp.Height = (int)(rect.Height * scale);
            }
            PlatformView.LayoutParameters = lp;
#endif
        }
#endif

        private static void MapCurrentExtent(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).CurrentExtent = TimeSlider.CurrentExtent;
#endif
        }

        private static void MapFullExtent(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtent = TimeSlider.FullExtent;
#endif
        }

        private static void MapTimeStepInterval(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).TimeStepInterval = TimeSlider.TimeStepInterval;
#endif
        }

        private static void MapTimeSteps(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).TimeSteps = TimeSlider.TimeSteps;
#endif
        }

        private static void MapPlaybackInterval(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackInterval = TimeSlider.PlaybackInterval;
#endif
        }

        private static void MapPlaybackDirection(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackDirection = TimeSlider.PlaybackDirection;
#endif
        }


        private static void MapPlaybackLoopMode(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackLoopMode = TimeSlider.PlaybackLoopMode;
#endif
        }


        private static void MapIsStartTimePinned(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).IsStartTimePinned = TimeSlider.IsStartTimePinned;
#endif
        }

        private static void MapIsEndTimePinned(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).IsEndTimePinned = TimeSlider.IsEndTimePinned;
#endif
        }


        private static void MapIsPlaying(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).IsPlaying = TimeSlider.IsPlaying;
#endif
        }

        private static void MapFullExtentLabelFormat(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtentLabelFormat = TimeSlider.FullExtentLabelFormat;
#endif
        }

        private static void MapCurrentExtentLabelFormat(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).CurrentExtentLabelFormat = TimeSlider.CurrentExtentLabelFormat;
#endif
        }

        private static void MapTimeStepIntervalLabelFormat(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).TimeStepIntervalLabelFormat = TimeSlider.TimeStepIntervalLabelFormat;
#endif
        }


        private static void MapLabelMode(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).LabelMode = TimeSlider.LabelMode;
#endif
        }


        private static void MapThumbStroke(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).ThumbStroke = TimeSlider.ThumbStroke;
#endif
        }

        private static void MapThumbFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).ThumbFill = TimeSlider.ThumbFill;
#endif
        }

        private static void MapCurrentExtentFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).CurrentExtentFill = TimeSlider.CurrentExtentFill;
#endif
        }

        private static void MapFullExtentFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtentFill = TimeSlider.FullExtentFill;
#endif
        }

        private static void MapFullExtentStroke(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtentStroke = TimeSlider.FullExtentStroke;
#endif
        }

        private static void MapTimeStepIntervalTickFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).TimeStepIntervalTickFill = TimeSlider.TimeStepIntervalTickFill;
#endif
        }

        private static void MapPlaybackButtonsFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackButtonsFill = TimeSlider.PlaybackButtonsFill;
#endif
        }

        private static void MapPlaybackButtonsStroke(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackButtonsStroke = TimeSlider.PlaybackButtonsStroke;
#endif
        }

        private static void MapFullExtentLabelColor(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtentLabelColor = TimeSlider.FullExtentLabelColor;
#endif
        }

        private static void MapCurrentExtentLabelColor(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).CurrentExtentLabelColor = TimeSlider.CurrentExtentLabelColor;
#endif
        }

        private static void MapTimeStepIntervalLabelColor(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).TimeStepIntervalLabelColor = TimeSlider.TimeStepIntervalLabelColor;
#endif
        }

        private bool _isUpdatingHeadingFromGeoView;

        /// <inheritdoc />
#if WINDOWS || __IOS__
        protected override NativeViewType CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider();
#elif __ANDROID__
        protected override NativeViewType CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider(this.Context);
#else
        protected override object CreatePlatformView()
        {
            throw new NotImplementedException();
        }
#endif
    }
}
