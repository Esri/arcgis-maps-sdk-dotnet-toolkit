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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Graphics;
using Android.Runtime;

#if NET6_0_OR_GREATER
using AndroidX.ConstraintLayout.Widget;
#else
using Android.Support.Constraints;
#endif
using Android.Util;
using Android.Views;
using View = Android.Views.View;
using Button = Android.Widget.Button;
using Android.Widget;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.Primitives;
using Esri.ArcGISRuntime.Toolkit.Maui;
//using Esri.ArcGISRuntime.Toolkit.Properties;
using Color = Android.Graphics.Color;
using RectF = Android.Graphics.RectF;
using Size = Android.Util.Size;
using ViewExtensions = Esri.ArcGISRuntime.Toolkit.Internal.ViewExtensions;
using Android.Content.Res;
using Android.Graphics.Drawables;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider")]
    [DisplayName("Time Slider")]
    [Category("ArcGIS Runtime Controls")]
    public partial class TimeSlider : ConstraintLayout, View.IOnTouchListener
    {
        private Context _context;
#pragma warning disable SX1309 // Names match elements in template
#pragma warning disable SA1306 // Names match elements in template
        private View? SliderTrack;
        private View? SliderTrackOutline;
        private View? MinimumThumb;
        private View? MaximumThumb;
        private View? PinnedMinimumThumb;
        private View? PinnedMaximumThumb;
        private View? HorizontalTrackThumb;
        private Button? NextButton;
        private Button? PreviousButton;
        private View? NextButtonOutline;
        private View? PreviousButtonOutline;
        private ToggleButton? PlayPauseButton;
        private View? PlayButtonOutline;
        private View? PauseButtonOutline;
        private RectangleView? SliderTrackStepBackRepeater = null;
        private RectangleView? SliderTrackStepForwardRepeater = null;
#pragma warning restore SX1309
#pragma warning restore SA1306
        private View? _startTimeTickmark;
        private View? _endTimeTickmark;
        private bool _isMinThumbFocused = false;
        private bool _isMaxThumbFocused = false;
        private float _lastX = 0;
        private ThrottleAwaiter _measureThrottler = new ThrottleAwaiter(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public TimeSlider(Context? context)
            : base(context)
        {
            InitializeImpl();
            Initialize();        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public TimeSlider(Context? context, IAttributeSet? attr)
            : base(context, attr)
        {
            _context = context;
            InitializeImpl();
            Initialize();
        }

        private void InitializeImpl()
        {
            if (DesignTime.IsDesignMode)
            {
                // Add placeholder text
                SetBackgroundColor(Color.LightGray);
                var designTimePlaceholderText = new TextView(Context)
                {
                    Text = "Time Slider",
                    TextSize = 16,
                    Id = 123456789,
                };
                designTimePlaceholderText.SetTextColor(Color.Black);
                AddView(designTimePlaceholderText);

                // Center text by constraining it to the edges of the parent view
                var constraintSet = new ConstraintSet();
                constraintSet.Clone(this);
                constraintSet.Connect(designTimePlaceholderText.Id, ConstraintSet.Start, ConstraintSet.ParentId, ConstraintSet.Start);
                constraintSet.Connect(designTimePlaceholderText.Id, ConstraintSet.End, ConstraintSet.ParentId, ConstraintSet.End);
                constraintSet.Connect(designTimePlaceholderText.Id, ConstraintSet.Top, ConstraintSet.ParentId, ConstraintSet.Top, 15);
                constraintSet.Connect(designTimePlaceholderText.Id, ConstraintSet.Bottom, ConstraintSet.ParentId, ConstraintSet.Bottom, 15);
                constraintSet.ApplyTo(this);

#pragma warning disable CS8774 // Member must have a non-null value when exiting.
                return;
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
            }

            //var inflater = LayoutInflater.FromContext(Context!);
            //inflater?.Inflate(Resource.Layout.TimeSlider, this, true);
            CreateUI();

            /*
            SliderTrack = FindViewById<View>(Resource.Id.SliderTrack);
            SliderTrackOutline = FindViewById<View>(Resource.Id.SliderTrackOutline);
            FullExtentStartTimeLabel = FindViewById<TextView>(Resource.Id.FullExtentStartTimeLabel);
            FullExtentEndTimeLabel = FindViewById<TextView>(Resource.Id.FullExtentEndTimeLabel);
            MinimumThumb = FindViewById<View>(Resource.Id.MinThumb);
            MaximumThumb = FindViewById<View>(Resource.Id.MaxThumb);
            MinimumThumbLabel = FindViewById<TextView>(Resource.Id.CurrentExtentStartTimeLabel);
            MaximumThumbLabel = FindViewById<TextView>(Resource.Id.CurrentExtentEndTimeLabel);
            PinnedMinimumThumb = FindViewById<View>(Resource.Id.PinnedMinThumb);
            PinnedMaximumThumb = FindViewById<View>(Resource.Id.PinnedMaxThumb);
            HorizontalTrackThumb = FindViewById<View>(Resource.Id.CurrentExtentFill);
            Tickmarks = FindViewById<Tickbar>(Resource.Id.Tickmarks);
            PlayPauseButton = FindViewById<ToggleButton>(Resource.Id.PlayPauseButton);
            PlayButtonOutline = FindViewById<View>(Resource.Id.PlayButtonOutline);
            PauseButtonOutline = FindViewById<View>(Resource.Id.PauseButtonOutline);
            NextButton = FindViewById<Button>(Resource.Id.NextButton);
            PreviousButton = FindViewById<Button>(Resource.Id.PreviousButton);
            NextButtonOutline = FindViewById<View>(Resource.Id.NextButtonOutline);
            PreviousButtonOutline = FindViewById<View>(Resource.Id.PreviousButtonOutline);
            _startTimeTickmark = FindViewById<View>(Resource.Id.FullExtentStartTimeTickmark);
            _endTimeTickmark = FindViewById<View>(Resource.Id.FullExtentEndTimeTickmark);
            */

            PositionTickmarks();
            ApplyLabelMode(LabelMode);

            if (PlayPauseButton != null)
            {
                PlayPauseButton.CheckedChange += (o, e) =>
                {
                    IsPlaying = PlayPauseButton.Checked;
                    if (PlayButtonOutline != null)
                    {
                        PlayButtonOutline.Visibility = IsPlaying ? ViewStates.Gone : ViewStates.Visible;
                    }

                    if (PauseButtonOutline != null)
                    {
                        PauseButtonOutline.Visibility = IsPlaying ? ViewStates.Visible : ViewStates.Gone;
                    }
                };
            }

            if (NextButton != null)
            {
                NextButton.Click += (o, e) => OnNextButtonClick();
            }

            if (PreviousButton != null)
            {
                PreviousButton.Click += (o, e) => OnPreviousButtonClick();
            }

            SetOnTouchListener(this);
        }

        private void CreateUI()
        {
            this.Id = GenerateViewId();

            // PlayPauseButton
            PlayPauseButton = new ToggleButton(_context) { Id = GenerateViewId(), TextOn = "", TextOff = "" };
            PlayPauseButton.LayoutParameters = new ViewGroup.LayoutParams(38.ToDips(Resources), 44.ToDips(Resources));
            AddView(PlayPauseButton);

            // PlayButtonOutline
            PlayButtonOutline = new View(_context) { Id = GenerateViewId() };
            PlayButtonOutline.LayoutParameters = new ViewGroup.LayoutParams(0, 0);
            AddView(PlayButtonOutline);

            // PauseButtonOutline
            PauseButtonOutline = new View (_context) { Id = GenerateViewId(), Visibility = ViewStates.Gone };
            PauseButtonOutline.LayoutParameters = new ViewGroup.LayoutParams(0, 0);
            AddView(PauseButtonOutline);

            // NextButton
            NextButton = new Button(_context) { Id = GenerateViewId() };
            PlayPauseButton.LayoutParameters = new LayoutParams(22.ToDips(Resources), 24.ToDips(Resources)) { LeftMargin = 10.ToDips(Resources)};
            AddView(NextButton);

            // NextButtonOutline
            NextButtonOutline = new View(_context) { Id = GenerateViewId() };
            NextButtonOutline.LayoutParameters = new ViewGroup.LayoutParams(0, 0);
            AddView(NextButtonOutline);

            // PreviousButton
            PreviousButton = new Button(_context) { Id = GenerateViewId(), Rotation = 180 };
            PlayPauseButton.LayoutParameters = new LayoutParams(22.ToDips(Resources), 24.ToDips(Resources)) { RightMargin = 13.ToDips(Resources)};
            AddView(PreviousButton);

            // PreviousButtonOutline
            PreviousButtonOutline = new View(_context) { Id = GenerateViewId(), Rotation = 180 };
            PreviousButtonOutline.LayoutParameters = new ViewGroup.LayoutParams(0, 0);
            AddView(PreviousButtonOutline);

            // FullExtentStartTimeLabel
            FullExtentStartTimeLabel = new TextView(_context) { Id = GenerateViewId(), Text = "7/1/2012", Clickable = false, Focusable = false};
            FullExtentStartTimeLabel.LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            AddView(FullExtentStartTimeLabel);

            // FullExtentStartGuide
            var fullExtentStartGuide = new Space(_context) { Id = GenerateViewId() };
            fullExtentStartGuide.LayoutParameters = new LayoutParams(2.ToDips(Resources), 1.ToDips(Resources));
            AddView(fullExtentStartGuide);

            // FullExtentEndTimeLabel
            FullExtentEndTimeLabel = new TextView(_context) { Id = GenerateViewId(), Text = "7/12/2012", Clickable = false, Focusable = false };
            FullExtentEndTimeLabel.LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            AddView(FullExtentEndTimeLabel);

            // FullExtentEndGuide
            var fullExtendEndGuide = new Space(_context) { Id = GenerateViewId() };
            fullExtendEndGuide.LayoutParameters = new LayoutParams(2.ToDips(Resources), 1.ToDips(Resources));
            AddView(fullExtendEndGuide);

            // SliderTrackOutline
            SliderTrackOutline = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Background = new ColorDrawable(Color.ParseColor("#5c5c5c")) };
            SliderTrackOutline.LayoutParameters = new LayoutParams(0, 6.ToDips(Resources))
            {
                LeftMargin = 0,
                RightMargin = 0,
                BottomMargin = 10.ToDips(Resources)
            };
            AddView(SliderTrackOutline);

            // SliderTrack
            SliderTrack = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Background = new ColorDrawable(Color.ParseColor("#5c5c5c"))};
            var m1 = 1.ToDips(Resources);
            SliderTrack.LayoutParameters = new LayoutParams(0, 0) { LeftMargin = m1, RightMargin = m1, TopMargin = m1, BottomMargin = m1};
            AddView(SliderTrack);

            // FullExtentStartTimeTickmark
            var fullExtentStartTimeTickmark = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Background = new ColorDrawable(Color.ParseColor("#5c5c5c")) };
            fullExtentStartTimeTickmark.SetForegroundGravity(GravityFlags.Center);
            fullExtentStartTimeTickmark.LayoutParameters = new LinearLayout.LayoutParams(m1, 0) { BottomMargin = m1, Gravity = GravityFlags.Center };
            AddView(fullExtentStartTimeTickmark);

            // FullExtentEndTimeTickmark
            var fullExtentEndTimeTickmark = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Background = new ColorDrawable(Color.ParseColor("#5c5c5c")) };
            fullExtentEndTimeTickmark.LayoutParameters = new LayoutParams(m1, 0) { BottomMargin = m1 };
            AddView(fullExtentEndTimeTickmark);
            
            // Tickmarks
            Tickmarks = new Tickbar(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false };
            Tickmarks.LayoutParameters = new LayoutParams(0, LayoutParams.WrapContent);
            AddView(Tickmarks);

            // ThumbGuideStart
            var thumbGuideStart = new Space(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false };
            thumbGuideStart.LayoutParameters = new LayoutParams(14.ToDips(Resources), 0);
            AddView(thumbGuideStart);

            // CurrentExtentFill
            HorizontalTrackThumb = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Background = new ColorDrawable(Color.ParseColor("#5E97F6")) };
            HorizontalTrackThumb.LayoutParameters = new LayoutParams(0, 0);
            AddView(HorizontalTrackThumb);

            // MinThumb
            MinimumThumb = new View (_context) { Id = GenerateViewId(), Clickable = false, Focusable = false };
            MinimumThumb.LayoutParameters = new LayoutParams(18.ToDips(Resources), 18.ToDips(Resources));
            AddView(MinimumThumb);

            // PinnedMinThumb
            PinnedMinimumThumb = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Visibility = ViewStates.Invisible, Background = new ColorDrawable(Color.ParseColor("#5e97f6"))};
            PinnedMinimumThumb.LayoutParameters = new LayoutParams(7.ToDips(Resources), 13.ToDips(Resources));
            AddView(PinnedMinimumThumb);

            // MinThumbCenter
            var minThumbCenter = new Space(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false };
            minThumbCenter.LayoutParameters = new LayoutParams(m1, m1);
            AddView(minThumbCenter);
            
            // MaxThumb
            MaximumThumb = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false };
            MaximumThumb.LayoutParameters = new LayoutParams(18.ToDips(Resources), 18.ToDips(Resources));
            AddView(MaximumThumb);

            // PinnedMaxThumb
            PinnedMaximumThumb = new View(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Visibility = ViewStates.Invisible, Background = new ColorDrawable(Color.ParseColor("#5e97f6"))};
            PinnedMaximumThumb.LayoutParameters = new LayoutParams(7.ToDips(Resources), 13.ToDips(Resources));
            AddView(PinnedMaximumThumb);

            // MaxThumbCenter
            var maxThumbCenter = new Space(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false };
            maxThumbCenter.LayoutParameters = new LayoutParams(m1, m1);
            AddView(maxThumbCenter);

            // CurrentExtentStartTimeLabel
            MinimumThumbLabel = new TextView(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Text = "7/4/2012" };
            MinimumThumbLabel.LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            {
                BottomMargin = 10.ToDips(Resources)
            };
            AddView(MinimumThumbLabel);

            // CurrentExtentEndTimeLabel
            MaximumThumbLabel = new TextView(_context) { Id = GenerateViewId(), Clickable = false, Focusable = false, Text = "7/8/2012" };
            MaximumThumbLabel.LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            {
                BottomMargin = 10.ToDips(Resources)
            };
            AddView(MaximumThumbLabel);

            // PlayPauseButton constraints
            ConstraintSet constraintSet = new ConstraintSet();
            constraintSet.Connect(PlayPauseButton.Id, ConstraintSet.Bottom, this.Id, ConstraintSet.Bottom);
            constraintSet.Connect(PlayPauseButton.Id, ConstraintSet.Start, this.Id, ConstraintSet.Start);
            constraintSet.Connect(PlayPauseButton.Id, ConstraintSet.End, this.Id, ConstraintSet.End);
            // PlayButtonOutline
            constraintSet.Connect(PlayButtonOutline.Id, ConstraintSet.Top, PlayPauseButton.Id, ConstraintSet.Top);
            constraintSet.Connect(PlayButtonOutline.Id, ConstraintSet.Bottom, PlayPauseButton.Id, ConstraintSet.Bottom);
            constraintSet.Connect(PlayButtonOutline.Id, ConstraintSet.Start, PlayPauseButton.Id, ConstraintSet.Start);
            constraintSet.Connect(PlayButtonOutline.Id, ConstraintSet.End, PlayPauseButton.Id, ConstraintSet.End);
            // PauseButtonOutline
            constraintSet.Connect(PauseButtonOutline.Id, ConstraintSet.Top, PlayPauseButton.Id, ConstraintSet.Top);
            constraintSet.Connect(PauseButtonOutline.Id, ConstraintSet.Bottom, PlayPauseButton.Id, ConstraintSet.Bottom);
            constraintSet.Connect(PauseButtonOutline.Id, ConstraintSet.Start, PlayPauseButton.Id, ConstraintSet.Start);
            constraintSet.Connect(PauseButtonOutline.Id, ConstraintSet.End, PlayPauseButton.Id, ConstraintSet.End);
            // NextButton
            constraintSet.Connect(NextButton.Id, ConstraintSet.Top, PlayPauseButton.Id, ConstraintSet.Top);
            constraintSet.Connect(NextButton.Id, ConstraintSet.Bottom, PlayPauseButton.Id, ConstraintSet.Bottom);
            constraintSet.Connect(NextButton.Id, ConstraintSet.Start, PlayPauseButton.Id, ConstraintSet.End);
            // NextButtonOutline
            constraintSet.Connect(NextButtonOutline.Id, ConstraintSet.Top, NextButton.Id, ConstraintSet.Top);
            constraintSet.Connect(NextButtonOutline.Id, ConstraintSet.Bottom, NextButton.Id, ConstraintSet.Bottom);
            constraintSet.Connect(NextButtonOutline.Id, ConstraintSet.Start, NextButton.Id, ConstraintSet.Start);
            constraintSet.Connect(NextButtonOutline.Id, ConstraintSet.End, NextButton.Id, ConstraintSet.End);
            // PreviousButton
            constraintSet.Connect(PreviousButton.Id, ConstraintSet.Top, PlayPauseButton.Id, ConstraintSet.Top);
            constraintSet.Connect(PreviousButton.Id, ConstraintSet.Bottom, PlayPauseButton.Id, ConstraintSet.Bottom);
            constraintSet.Connect(PreviousButton.Id, ConstraintSet.End, PlayPauseButton.Id, ConstraintSet.Start);
            // PreviousButtonOutline
            constraintSet.Connect(PreviousButtonOutline.Id, ConstraintSet.Top, PreviousButton.Id, ConstraintSet.Top);
            constraintSet.Connect(PreviousButtonOutline.Id, ConstraintSet.Bottom, PreviousButton.Id, ConstraintSet.Bottom);
            constraintSet.Connect(PreviousButtonOutline.Id, ConstraintSet.Start, PreviousButton.Id, ConstraintSet.Start);
            constraintSet.Connect(PreviousButtonOutline.Id, ConstraintSet.End, PreviousButton.Id, ConstraintSet.End);
            // FullExtentStartTimeLabel
            constraintSet.Connect(FullExtentStartTimeLabel.Id, ConstraintSet.Top, PlayPauseButton.Id, ConstraintSet.Top);
            constraintSet.Connect(FullExtentStartTimeLabel.Id, ConstraintSet.Start, this.Id, ConstraintSet.Start);
            // FullExtentStartGuide
            constraintSet.Connect(fullExtentStartGuide.Id, ConstraintSet.Start, FullExtentStartTimeLabel.Id, ConstraintSet.Start);
            constraintSet.Connect(fullExtentStartGuide.Id, ConstraintSet.End, FullExtentStartTimeLabel.Id, ConstraintSet.End);
            constraintSet.Connect(fullExtentStartGuide.Id, ConstraintSet.Bottom, FullExtentStartTimeLabel.Id, ConstraintSet.Top);
            // FullExtentEndTimeLabel
            constraintSet.Connect(FullExtentEndTimeLabel.Id, ConstraintSet.Top, PlayPauseButton.Id, ConstraintSet.Top);
            constraintSet.Connect(FullExtentEndTimeLabel.Id, ConstraintSet.End, this.Id, ConstraintSet.End);
            // FullExtentEndGuide
            constraintSet.Connect(fullExtendEndGuide.Id, ConstraintSet.Start, FullExtentEndTimeLabel.Id, ConstraintSet.Start);
            constraintSet.Connect(fullExtendEndGuide.Id, ConstraintSet.End, FullExtentEndTimeLabel.Id, ConstraintSet.End);
            constraintSet.Connect(fullExtendEndGuide.Id, ConstraintSet.Bottom, FullExtentEndTimeLabel.Id, ConstraintSet.Top);
            // SliderTrackOutline
            constraintSet.Connect(SliderTrackOutline.Id, ConstraintSet.Start, fullExtentStartGuide.Id, ConstraintSet.Start);
            constraintSet.Connect(SliderTrackOutline.Id, ConstraintSet.End, fullExtendEndGuide.Id, ConstraintSet.End);
            constraintSet.Connect(SliderTrackOutline.Id, ConstraintSet.Bottom, PlayPauseButton.Id, ConstraintSet.Top);
            // SliderTrack
            constraintSet.Connect(SliderTrack.Id, ConstraintSet.Start, SliderTrackOutline.Id, ConstraintSet.Start);
            constraintSet.Connect(SliderTrack.Id, ConstraintSet.End, SliderTrackOutline.Id, ConstraintSet.End);
            constraintSet.Connect(SliderTrack.Id, ConstraintSet.Top, SliderTrackOutline.Id, ConstraintSet.Top);
            constraintSet.Connect(SliderTrack.Id, ConstraintSet.Bottom, SliderTrackOutline.Id, ConstraintSet.Bottom);
            // FullExtentStartTimeTickmark
            constraintSet.Connect(fullExtentStartTimeTickmark.Id, ConstraintSet.Bottom, FullExtentStartTimeLabel.Id, ConstraintSet.Top);
            constraintSet.Connect(fullExtentStartTimeTickmark.Id, ConstraintSet.Top, SliderTrackOutline.Id, ConstraintSet.Bottom);
            constraintSet.Connect(fullExtentStartTimeTickmark.Id, ConstraintSet.Start, SliderTrackOutline.Id, ConstraintSet.Start);
            // FullExtentEndTimeTickmark
            constraintSet.Connect(fullExtentEndTimeTickmark.Id, ConstraintSet.Bottom, FullExtentEndTimeLabel.Id, ConstraintSet.Top);
            constraintSet.Connect(fullExtentEndTimeTickmark.Id, ConstraintSet.Top, SliderTrackOutline.Id, ConstraintSet.Bottom);
            constraintSet.Connect(fullExtentEndTimeTickmark.Id, ConstraintSet.End, SliderTrackOutline.Id, ConstraintSet.End);
            // Tickmarks
            constraintSet.Connect(Tickmarks.Id, ConstraintSet.Start, this.Id, ConstraintSet.Start);
            constraintSet.Connect(Tickmarks.Id, ConstraintSet.End, this.Id, ConstraintSet.End);
            constraintSet.Connect(Tickmarks.Id, ConstraintSet.Bottom, SliderTrackOutline.Id, ConstraintSet.Top);
            // ThumbGuideStart
            constraintSet.Connect(thumbGuideStart.Id, ConstraintSet.End, SliderTrack.Id, ConstraintSet.Start);
            constraintSet.Connect(thumbGuideStart.Id, ConstraintSet.Top, SliderTrack.Id, ConstraintSet.Top);
            constraintSet.Connect(thumbGuideStart.Id, ConstraintSet.Bottom, SliderTrack.Id, ConstraintSet.Bottom);
            // CurrentExtentFill
            constraintSet.Connect(HorizontalTrackThumb.Id, ConstraintSet.Start, SliderTrack.Id, ConstraintSet.Start);
            constraintSet.Connect(HorizontalTrackThumb.Id, ConstraintSet.Top, SliderTrack.Id, ConstraintSet.Top);
            constraintSet.Connect(HorizontalTrackThumb.Id, ConstraintSet.Bottom, SliderTrack.Id, ConstraintSet.Bottom);
            // MinThumb
            constraintSet.Connect(MinimumThumb.Id, ConstraintSet.Start, this.Id, ConstraintSet.Start);
            constraintSet.Connect(MinimumThumb.Id, ConstraintSet.Top, SliderTrack.Id, ConstraintSet.Top);
            constraintSet.Connect(MinimumThumb.Id, ConstraintSet.Bottom, SliderTrack.Id, ConstraintSet.Bottom);
            // PinnedMinThumb
            constraintSet.Connect(PinnedMinimumThumb.Id, ConstraintSet.Start, MinimumThumb.Id, ConstraintSet.Start);
            constraintSet.Connect(PinnedMinimumThumb.Id, ConstraintSet.End, MinimumThumb.Id, ConstraintSet.End);
            constraintSet.Connect(PinnedMinimumThumb.Id, ConstraintSet.Top, MinimumThumb.Id, ConstraintSet.Top);
            constraintSet.Connect(PinnedMinimumThumb.Id, ConstraintSet.Bottom, MinimumThumb.Id, ConstraintSet.Bottom);
            // MinThumbCenter
            constraintSet.Connect(minThumbCenter.Id, ConstraintSet.Start, MinimumThumb.Id, ConstraintSet.Start);
            constraintSet.Connect(minThumbCenter.Id, ConstraintSet.End, MinimumThumb.Id, ConstraintSet.End);
            constraintSet.Connect(minThumbCenter.Id, ConstraintSet.Top, SliderTrack.Id, ConstraintSet.Top);
            constraintSet.Connect(minThumbCenter.Id, ConstraintSet.Bottom, SliderTrack.Id, ConstraintSet.Bottom);
            // MaxThumb
            constraintSet.Connect(MaximumThumb.Id, ConstraintSet.Start, this.Id, ConstraintSet.Start);
            constraintSet.Connect(MaximumThumb.Id, ConstraintSet.Top, SliderTrack.Id, ConstraintSet.Top);
            constraintSet.Connect(MaximumThumb.Id, ConstraintSet.Bottom, SliderTrack.Id, ConstraintSet.Bottom);
            // PinnedMaxThumb
            constraintSet.Connect(PinnedMaximumThumb.Id, ConstraintSet.Start, MaximumThumb.Id, ConstraintSet.Start);
            constraintSet.Connect(PinnedMaximumThumb.Id, ConstraintSet.End, MaximumThumb.Id, ConstraintSet.End);
            constraintSet.Connect(PinnedMaximumThumb.Id, ConstraintSet.Top, MaximumThumb.Id, ConstraintSet.Top);
            constraintSet.Connect(PinnedMaximumThumb.Id, ConstraintSet.Bottom, MaximumThumb.Id, ConstraintSet.Bottom);
            // MaxThumbCenter
            constraintSet.Connect(maxThumbCenter.Id, ConstraintSet.Start, MaximumThumb.Id, ConstraintSet.Start);
            constraintSet.Connect(maxThumbCenter.Id, ConstraintSet.End, MaximumThumb.Id, ConstraintSet.End);
            constraintSet.Connect(maxThumbCenter.Id, ConstraintSet.Top, SliderTrack.Id, ConstraintSet.Top);
            constraintSet.Connect(maxThumbCenter.Id, ConstraintSet.Bottom, SliderTrack.Id, ConstraintSet.Bottom);
            // CurrentExtentStartTimeLabel
            constraintSet.Connect(MinimumThumbLabel.Id, ConstraintSet.Bottom, minThumbCenter.Id, ConstraintSet.Top);
            constraintSet.Connect(MinimumThumbLabel.Id, ConstraintSet.Start, this.Id, ConstraintSet.Start);
            // CurrentExtentEndTimeLabel
            constraintSet.Connect(MaximumThumbLabel.Id, ConstraintSet.Bottom, maxThumbCenter.Id, ConstraintSet.Top);
            constraintSet.Connect(MaximumThumbLabel.Id, ConstraintSet.Start, this.Id, ConstraintSet.Start);
            // Apply all constraints
            constraintSet.ApplyTo(this);
        }

        private void InvalidateMeasureAndArrange()
        {
            if (CurrentValidExtent != null)
            {
                UpdateTrackLayout(CurrentValidExtent);
            }
        }

        private Size GetDesiredSize(View view)
        {
            view.Measure((int)MeasureSpecMode.Unspecified, (int)MeasureSpecMode.Unspecified);
            return new Size((int)view.MeasuredWidth, (int)view.MeasuredHeight);
        }

        /// <inheritdoc />
        public bool OnTouch(View? v, MotionEvent? e)
        {
            if (e is null || MinimumThumb is null || MaximumThumb is null)
            {
                return false;
            }

            // Get x/y coordinates of touch location
            var touchX = e.RawX;
            var touchY = e.RawY;

            var isTouchHandled = _isMinThumbFocused || _isMaxThumbFocused;
            var minTargetSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 44f, ViewExtensions.GetDisplayMetrics(Context));
            var minThumbBounds = GetBounds(MinimumThumb, minTargetSize, minTargetSize);
            var maxThumbBounds = GetBounds(MaximumThumb, minTargetSize, minTargetSize);
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    _isMinThumbFocused = minThumbBounds.Contains(touchX, touchY);
                    _isMaxThumbFocused = maxThumbBounds.Contains(touchX, touchY);

                    if (_isMinThumbFocused && !IsStartTimePinned)
                    {
                        _lastX = touchX - minThumbBounds.Left;
                        isTouchHandled = true;
                    }

                    if (_isMaxThumbFocused && !IsEndTimePinned)
                    {
                        _lastX = touchX - maxThumbBounds.Left;
                        isTouchHandled = true;
                    }

                    break;
                case MotionEventActions.Move:
                    if (!_isMinThumbFocused && !_isMaxThumbFocused && !(_isMinThumbFocused && IsStartTimePinned) && !(_isMaxThumbFocused && IsEndTimePinned))
                    {
                        return isTouchHandled;
                    }

                    View? trackedThumb = null;
                    if (_isMinThumbFocused && _isMaxThumbFocused)
                    {
                        var maxThumbTranslateX = touchX - maxThumbBounds.Left - _lastX;

                        // Gesture was within both min and max thumb, so let the direction of the gesture determine which thumb should be dragged
                        if (maxThumbTranslateX < 0)
                        {
                            // Gesture is moving thumb toward the min, so put focus on min thumb
                            trackedThumb = MinimumThumb;
                            _isMaxThumbFocused = false;
                        }
                        else
                        {
                            // Gesture is moving thumb toward the max, so put focus on max thumb
                            trackedThumb = MaximumThumb;
                            _isMinThumbFocused = false;
                        }
                    }
                    else if (_isMinThumbFocused)
                    {
                        trackedThumb = MinimumThumb;
                    }
                    else if (_isMaxThumbFocused)
                    {
                        trackedThumb = MaximumThumb;
                    }

                    var currentThumbX = touchX - trackedThumb?.Left ?? 0;
                    var translateX = currentThumbX - _lastX;

                    if (_isMinThumbFocused)
                    {
                        OnMinimumThumbDrag(translateX);
                    }

                    if (_isMaxThumbFocused)
                    {
                        OnMaximumThumbDrag(translateX);
                    }

                    break;
                case MotionEventActions.Up:
                    _isMinThumbFocused = false;
                    _isMaxThumbFocused = false;
                    OnDragCompleted();
                    break;
            }

            return isTouchHandled;
        }

        private RectF GetBounds(View view, float minWidth, float minHeight)
        {
            var xy = new int[2];
            view.GetLocationOnScreen(xy);
            var left = xy[0];
            var right = left + view.GetActualWidth();
            var top = xy[1];
            var bottom = top + view.GetActualHeight();

            var width = right - left;
            if (width < minWidth)
            {
                var expansionFactor = (int)((minWidth - width) / 2);
                left -= expansionFactor;
                right += expansionFactor;
            }

            var height = bottom - top;
            if (height < minHeight)
            {
                var expansionFactor = (int)((minHeight - height) / 2);
                top -= expansionFactor;
                bottom += expansionFactor;
            }

            return new RectF((float)left, (float)top, (float)right, (float)bottom);
        }

        /// <inheritdoc />
        protected async override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            await _measureThrottler.ThrottleDelay();
            if (Tickmarks != null && SliderTrack != null)
            {
                Tickmarks.TickInset = (float)(this.GetActualWidth() - SliderTrack.GetActualWidth()) / 2;
            }
        }
    }
    internal static class LayoutExtension
    {
        public static int ToDips(this int input, Resources Resources)
        {
            return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, input, Resources.DisplayMetrics);
        }
    }
}