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
#if WINDOWS || __IOS__ || __ANDROID__
using Esri.ArcGISRuntime.Maui.Handlers;
using Microsoft.Maui.Handlers;
using System.ComponentModel;
using System.Runtime.InteropServices;
#if WINDOWS
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.TimeExtentChangedEventArgs;
#elif __IOS__
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.Maui.TimeExtentChangedEventArgs;
using Esri.ArcGISRuntime.Toolkit.Maui;
#elif __ANDROID__
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.Maui.TimeExtentChangedEventArgs;
using Esri.ArcGISRuntime.Toolkit.Maui;
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
        public TimeSliderHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper ?? TimeSliderMapper, commandMapper)
        {
        }

#if WINDOWS || __IOS__ || __ANDROID__
        /// <inheritdoc />
        protected override void ConnectHandler(NativeViewType platformView)
        {
            base.ConnectHandler(platformView);
            if (platformView is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += PlatformView_PropertyChanged;
            }
            platformView.CurrentExtentChanged += PlatformView_CurrentExtentChanged;
            //UpdateHeadingFromNativeTimeSlider(VirtualView);
            // TODO = fill for all properties
        }

        private void PlatformView_CurrentExtentChanged(object sender, TimeExtentChangedEventArg e)
        {
            VirtualView.InvokeEventChanged(sender, e);
        }

        private void PlatformView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _isUpdatingHeadingFromGeoView = true;
            var view = PlatformView as NativeViewType;
            var thisTS = VirtualView as ITimeSlider;

            if (view == null || thisTS == null)
            {
                return;
            }

            switch (e.PropertyName)
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
                    thisTS.PlaybackDirection = (PlaybackDirection)view.PlaybackDirection;
                    break;
                case nameof(ITimeSlider.PlaybackLoopMode):
                    thisTS.PlaybackLoopMode = (LoopMode)view.PlaybackLoopMode;
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
                    thisTS.LabelMode = (TimeSliderLabelMode)view.LabelMode;
                    break;
                case nameof(ITimeSlider.ThumbStroke):
                    thisTS.ThumbStroke = view.ThumbStroke.ToMaui();
                    break;
                case nameof(ITimeSlider.ThumbFill):
                    thisTS.ThumbFill = view.ThumbFill.ToMaui();
                    break;
                case nameof(ITimeSlider.CurrentExtentFill):
                    thisTS.CurrentExtentFill = view.CurrentExtentFill.ToMaui();
                    break;
                case nameof(ITimeSlider.FullExtentFill):
                    thisTS.FullExtentFill = view.FullExtentFill.ToMaui();
                    break;
                case nameof(ITimeSlider.FullExtentStroke):
                    thisTS.FullExtentStroke = view.FullExtentStroke.ToMaui();
                    break;
                case nameof(ITimeSlider.TimeStepIntervalTickFill):
                    thisTS.TimeStepIntervalTickFill = view.TimeStepIntervalTickFill.ToMaui();
                    break;
                case nameof(ITimeSlider.PlaybackButtonsFill):
                    thisTS.PlaybackButtonsFill = view.PlaybackButtonsFill.ToMaui();
                    break;
                case nameof(ITimeSlider.PlaybackButtonsStroke):
                    thisTS.PlaybackButtonsStroke = view.PlaybackButtonsStroke.ToMaui();
                    break;
                case nameof(ITimeSlider.FullExtentLabelColor):
                    thisTS.FullExtentLabelColor = view.FullExtentLabelColor.ToMaui();
                    break;
                case nameof(ITimeSlider.CurrentExtentLabelColor):
                    thisTS.CurrentExtentLabelColor = view.CurrentExtentLabelColor.ToMaui();
                    break;
                case nameof(ITimeSlider.TimeStepIntervalLabelColor):
                    thisTS.TimeStepIntervalLabelColor = view.TimeStepIntervalLabelColor.ToMaui();
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
#if __IOS__ || __ANDROID__
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
#if __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)
                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackDirection = (PlaybackDirection)TimeSlider.PlaybackDirection;
#elif WINDOWS
            if (!handler._isUpdatingHeadingFromGeoView)
                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackDirection = (Toolkit.PlaybackDirection)TimeSlider.PlaybackDirection;
#endif
        }


        private static void MapPlaybackLoopMode(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)
                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackLoopMode = (LoopMode)TimeSlider.PlaybackLoopMode;
#elif WINDOWS
            if (!handler._isUpdatingHeadingFromGeoView)
                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackLoopMode = (Toolkit.LoopMode)TimeSlider.PlaybackLoopMode;
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

                ((NativeViewType)handler.PlatformView).CurrentExtentLabelFormat = TimeSlider.CurrentExtentLabelFormat;
#endif
        }

        private static void MapTimeStepIntervalLabelFormat(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((NativeViewType)handler.PlatformView).TimeStepIntervalLabelFormat = TimeSlider.TimeStepIntervalLabelFormat;
#endif
        }


        private static void MapLabelMode(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)
                ((NativeViewType)handler.PlatformView).LabelMode = (TimeSliderLabelMode)TimeSlider.LabelMode;
#elif WINDOWS
            if (!handler._isUpdatingHeadingFromGeoView)
                ((NativeViewType)handler.PlatformView).LabelMode = (Toolkit.TimeSliderLabelMode)TimeSlider.LabelMode;
#endif
        }


        private static void MapThumbStroke(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).ThumbStroke = TimeSlider.ThumbStroke.ToNativeColor();
#endif
        }

        private static void MapThumbFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).ThumbFill = TimeSlider.ThumbFill.ToNativeColor();
#endif
        }

        private static void MapCurrentExtentFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).CurrentExtentFill = TimeSlider.CurrentExtentFill.ToNativeColor();
#endif
        }

        private static void MapFullExtentFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtentFill = TimeSlider.FullExtentFill.ToNativeColor();
#endif
        }

        private static void MapFullExtentStroke(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtentStroke = TimeSlider.FullExtentStroke.ToNativeColor();
#endif
        }

        private static void MapTimeStepIntervalTickFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).TimeStepIntervalTickFill = TimeSlider.TimeStepIntervalTickFill.ToNativeColor();
#endif
        }

        private static void MapPlaybackButtonsFill(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackButtonsFill = TimeSlider.PlaybackButtonsFill.ToNativeColor();
#endif
        }

        private static void MapPlaybackButtonsStroke(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).PlaybackButtonsStroke = TimeSlider.PlaybackButtonsStroke.ToNativeColor();
#endif
        }

        private static void MapFullExtentLabelColor(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).FullExtentLabelColor = TimeSlider.FullExtentLabelColor.ToNativeColor();
#endif
        }

        private static void MapCurrentExtentLabelColor(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).CurrentExtentLabelColor = TimeSlider.CurrentExtentLabelColor.ToNativeColor();
#endif
        }

        private static void MapTimeStepIntervalLabelColor(TimeSliderHandler handler, ITimeSlider TimeSlider)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            if (!handler._isUpdatingHeadingFromGeoView)

                ((Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider)handler.PlatformView).TimeStepIntervalLabelColor = TimeSlider.TimeStepIntervalLabelColor.ToNativeColor();
#endif
        }

        private bool _isUpdatingHeadingFromGeoView;

        /// <inheritdoc />
#if WINDOWS || __IOS__
        protected override NativeViewType CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider();
#elif __ANDROID__
        protected override NativeViewType CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider(this.Context);
#else
        protected override NativeViewType CreatePlatformView()
        {
            throw new NotImplementedException();
        }
#endif


    }
    internal static class ColorExtension
    {
#if __IOS__
        public static UIKit.UIColor ToNativeColor(this Microsoft.Maui.Graphics.Color? input)
        {
            if (input == null)
            {
                return UIKit.UIColor.Black;
            }
            return UIKit.UIColor.FromRGBA(input.Red, input.Green, input.Blue, input.Alpha);
        }
        public static Color ToMaui(this UIKit.UIColor? input)
        {
            if (input == null)
            {
                return Color.FromRgba("#000");
            }
            NFloat r, g, b;
            input.GetRGBA(out r, out g, out b, out _);
            return Color.FromRgb(r, g, b);
        }

#elif __ANDROID__
        public static Android.Graphics.Color ToNativeColor(this Microsoft.Maui.Graphics.Color? input)
        {
            if (input == null)
            {
                return Android.Graphics.Color.Black;
            }

            return Android.Graphics.Color.Argb((int)(input.Alpha * 255), (int)(input.Red * 255), (int)(input.Green * 255), (int)(input.Blue * 255));
        }

        public static Color ToMaui(this Android.Graphics.Color input)
        {
            return Color.FromRgb(input.R, input.G, input.B);
        }
#elif WINDOWS
        public static Microsoft.UI.Xaml.Media.Brush ToNativeColor(this Microsoft.Maui.Graphics.Color? input)
        {
            if (input == null)
            {
                return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
            }
            byte r, g, b, a;
            input.ToRgba(out r, out g, out b, out a);
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
        }
        public static Color ToMaui(this Microsoft.UI.Xaml.Media.Brush brush)
        {
            if (brush is Microsoft.UI.Xaml.Media.SolidColorBrush input)
            {
                return new Color(input.Color.R, input.Color.G, input.Color.B);
            };
            return Color.FromRgb(255, 255, 255);
        }
#endif
    }
}
#endif