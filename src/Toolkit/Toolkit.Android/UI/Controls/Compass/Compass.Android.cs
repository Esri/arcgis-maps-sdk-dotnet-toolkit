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

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass")]
    public partial class Compass
    {
        private static DisplayMetrics s_displayMetrics;
        private static IWindowManager s_windowManager;
        private NorthArrowShape _northArrow;
        private ViewPropertyAnimator _fadeInAnimation;
        private ViewPropertyAnimator _fadeOutAnimation;

        /// <summary>
        /// Initializes a new instance of the <see cref="Compass"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public Compass(Context context)
            : base(context) => Initialize();

        /// <summary>
        /// Initializes a new instance of the <see cref="Compass"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public Compass(Context context, IAttributeSet attr)
            : base(context, attr) => Initialize();

        /// <inheritdoc />
        protected override LayoutParams GenerateDefaultLayoutParams()
        {
            var size = (int)CalculateScreenDimension((float)DefaultSize);
            return new LayoutParams(size, size);
        }

        private void Initialize()
        {
            var size = (int)CalculateScreenDimension((float)DefaultSize);
            _northArrow = new NorthArrowShape(Context) { Size = size };
            _northArrow.LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(Android.Widget.FrameLayout.LayoutParams.MatchParent, Android.Widget.FrameLayout.LayoutParams.MatchParent);
            AddView(_northArrow);
            UpdateCompassRotation(false);
            _northArrow.Click += (s, e) => ResetRotation();
        }

        private void UpdateCompassRotation(bool transition)
        {
            SetVisibility(!AutoHide || Heading != 0, transition);
            _northArrow.Rotation = -(float)Heading;
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            MeasureChild(_northArrow, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(widthMeasureSpec), MeasureSpecMode.AtMost), MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

            // Calculate the ideal width and height for the view
            var desiredWidth = PaddingLeft + PaddingRight + _northArrow.MeasuredWidth;
            var desiredHeight = PaddingTop + PaddingBottom + _northArrow.MeasuredHeight;

            // Get the width and height of the view given any width and height constraints indicated by the width and height spec values
            var width = ResolveSize(desiredWidth, widthMeasureSpec);
            var height = ResolveSize(desiredHeight, heightMeasureSpec);
            SetMeasuredDimension(width, height);
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // Forward layout call to the root layout
            _northArrow.Layout(PaddingLeft, PaddingTop, _northArrow.MeasuredWidth + PaddingLeft, _northArrow.MeasuredHeight + PaddingBottom);
        }

        private void SetVisibility(bool isVisible, bool animate = true)
        {
            if (animate)
            {
                if (isVisible)
                {
                    if (_fadeInAnimation != null)
                    {
                        return; // Already fading in
                    }

                    if (_fadeOutAnimation != null)
                    {
                        _fadeOutAnimation.Cancel();
                        _fadeOutAnimation = null;
                    }

                    _fadeInAnimation = _northArrow.Animate().Alpha(1f).SetDuration(250).WithEndAction(new Java.Lang.Runnable(() => { _fadeInAnimation = null; }));
                }
                else
                {
                    if (_fadeOutAnimation != null)
                    {
                        return; // Already fading out
                    }

                    if (_fadeInAnimation != null)
                    {
                        _fadeInAnimation.Cancel();
                        _fadeInAnimation = null;
                    }

                    _fadeOutAnimation = _northArrow.Animate().Alpha(0f).SetDuration(250).WithEndAction(new Java.Lang.Runnable(() => { _fadeOutAnimation = null; }));
                }
            }
            else
            {
                _northArrow.Alpha = isVisible ? 1f : .0f;
            }
        }

        // Gets a display metrics object for calculating display dimensions
        private static DisplayMetrics GetDisplayMetrics()
        {
            if (s_displayMetrics == null)
            {
                if (s_windowManager == null)
                {
                    s_windowManager = Application.Context?.GetSystemService(Context.WindowService)?.JavaCast<IWindowManager>();
                }

                if (s_windowManager == null)
                {
                    s_displayMetrics = Application.Context?.Resources?.DisplayMetrics;
                }
                else
                {
                    s_displayMetrics = new DisplayMetrics();
                    s_windowManager.DefaultDisplay.GetMetrics(s_displayMetrics);
                }
            }

            return s_displayMetrics;
        }

        // Calculates a screen dimension given a specified dimension in raw pixels
        internal static float CalculateScreenDimension(float pixels, ComplexUnitType screenUnitType = ComplexUnitType.Dip)
        {
            return !DesignTime.IsDesignMode ?
                TypedValue.ApplyDimension(screenUnitType, pixels, GetDisplayMetrics()) : pixels;
        }

        private class NorthArrowShape : View
        {
            internal NorthArrowShape(Context context)
                : base(context)
            {
                SetWillNotDraw(false);
            }

            /// <inheritdoc />
            protected override void OnDraw(Canvas canvas)
            {
                float size = MeasuredWidth > MeasuredHeight ? MeasuredHeight : MeasuredWidth;
                var strokeWidth = Compass.CalculateScreenDimension(1.5f, ComplexUnitType.Dip);
                float c = size * .5f;
                float l = (MeasuredWidth - size) * .5f;
                float t = (MeasuredHeight - size) * .5f;

                Paint paint = new Paint(PaintFlags.AntiAlias);
                paint.SetStyle(Paint.Style.Fill);
                paint.SetARGB(255, 51, 51, 51);
                canvas.DrawCircle(c + l, c + t, size * .5f, paint);
                paint.SetStyle(Paint.Style.Stroke);
                paint.StrokeWidth = strokeWidth;
                paint.SetARGB(255, 255, 255, 255);
                canvas.DrawCircle(c + l, c + t, (size * .5f) - (strokeWidth / 2f), paint);

                // Draw north arrow
                paint.SetStyle(Paint.Style.Fill);
                paint.SetARGB(255, 199, 85, 46);
                var path = new Path();
                path.MoveTo(c - (size * .14f) + l, c + t);
                path.LineTo(c + (size * .14f) + l, c + t);
                path.LineTo(c + l, c - (size * .34f) + t);
                path.Close();
                canvas.DrawPath(path, paint);

                // Draw south arrow
                paint.SetARGB(255, 255, 255, 255);
                path = new Path();
                path.MoveTo(c - (size * .14f) + l, c + t);
                path.LineTo(c + (size * .14f) + l, c + t);
                path.LineTo(c + l, c + (size * .34f) + t);
                path.Close();
                canvas.DrawPath(path, paint);

                PivotX = (size / 2) + l;
                PivotY = (size / 2) + t;

                base.OnDraw(canvas);
            }

            public float Size { get; set; } = (float)DefaultSize;
        }
    }
}