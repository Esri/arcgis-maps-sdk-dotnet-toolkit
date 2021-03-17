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
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider")]
    [DisplayName("Time Slider")]
    [Category("ArcGIS Runtime Controls")]
    public partial class TimeSlider : ConstraintLayout, View.IOnTouchListener
    {
#pragma warning disable SX1309 // Names match elements in template
#pragma warning disable SA1306 // Names match elements in template
        private View SliderTrack;
        private View SliderTrackOutline;
        private View MinimumThumb;
        private View MaximumThumb;
        private View PinnedMinimumThumb;
        private View PinnedMaximumThumb;
        private View HorizontalTrackThumb;
        private Button NextButton;
        private Button PreviousButton;
        private View NextButtonOutline;
        private View PreviousButtonOutline;
        private ToggleButton PlayPauseButton;
        private View PlayButtonOutline;
        private View PauseButtonOutline;
        private RectangleView SliderTrackStepBackRepeater = null;
        private RectangleView SliderTrackStepForwardRepeater = null;
#pragma warning restore SX1309
#pragma warning restore SA1306
        private View _startTimeTickmark;
        private View _endTimeTickmark;
        private bool _isMinThumbFocused = false;
        private bool _isMaxThumbFocused = false;
        private float _lastX = 0;
        private ThrottleAwaiter _measureThrottler = new ThrottleAwaiter(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public TimeSlider(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public TimeSlider(Context context, IAttributeSet attr)
            : base(context, attr)
        {
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

                return;
            }

            var inflater = LayoutInflater.FromContext(Context);
            inflater.Inflate(Resource.Layout.TimeSlider, this, true);

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

            PositionTickmarks();
            ApplyLabelMode(LabelMode);

            PlayPauseButton.CheckedChange += (o, e) =>
            {
                IsPlaying = PlayPauseButton.Checked;
                PlayButtonOutline.Visibility = IsPlaying ? ViewStates.Gone : ViewStates.Visible;
                PauseButtonOutline.Visibility = IsPlaying ? ViewStates.Visible : ViewStates.Gone;
            };
            NextButton.Click += (o, e) => OnNextButtonClick();
            PreviousButton.Click += (o, e) => OnPreviousButtonClick();

            SetOnTouchListener(this);
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
        public bool OnTouch(View v, MotionEvent e)
        {
            // Get x/y coordinates of touch location
            var touchX = e.RawX;
            var touchY = e.RawY;

            var isTouchHandled = _isMinThumbFocused || _isMaxThumbFocused;
            var minTargetSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 44f, ViewExtensions.GetDisplayMetrics());
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

                    View trackedThumb = null;
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

                    var currentThumbX = touchX - trackedThumb.Left;
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
            Tickmarks.TickInset = (float)(this.GetActualWidth() - SliderTrack.GetActualWidth()) / 2;
        }
    }
}