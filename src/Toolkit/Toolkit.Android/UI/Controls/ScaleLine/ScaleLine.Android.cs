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
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.ScaleLine")]
    public partial class ScaleLine
    {
        private static DisplayMetrics s_displayMetrics;
        private static IWindowManager s_windowManager;
        private RectangleView _firstMetricTickLine;
        private RectangleView _secondMetricTickLine;
        private RectangleView _scaleLineStartSegment;
        private RectangleView _firstUsTickLine;
        private RectangleView _secondUsTickLine;
        private RectangleView _combinedScaleLine;
        private RectangleView _metricWidthPlaceholder;
        private RectangleView _usWidthPlaceholder;
        private LinearLayout _rootLayout;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleLine"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public ScaleLine(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleLine"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public ScaleLine(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        private void Initialize()
        {
            if (!DesignTime.IsDesignMode)
            {
                TargetWidth = CalculateScreenDimension(200);
            }

            // Vertically-oriented layout for containing all scalebar components
            _rootLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
            };
            _rootLayout.SetGravity(GravityFlags.Top);

            // Initialize scalebar components with placeholder sizes and values
            var scalelineHeight = CalculateScreenDimension(2);
            _combinedScaleLine = new RectangleView(Context, TargetWidth, scalelineHeight) { BackgroundColor = ForegroundColor };
            _metricScaleLine = new RectangleView(Context, TargetWidth, 1);
            _metricValue = new TextView(Context) { Text = "100" };
            _metricUnit = new TextView(Context) { Text = "m" };
            _usScaleLine = new RectangleView(Context, _metricScaleLine.Width * .9144, 1);
            _usValue = new TextView(Context) { Text = "300" };
            _usUnit = new TextView(Context) { Text = "ft" };

            var fontSize = 12;
            _metricValue.SetTextSize(ComplexUnitType.Dip, fontSize);
            _metricValue.SetTextColor(ForegroundColor);
            _metricUnit.SetTextSize(ComplexUnitType.Dip, fontSize);
            _metricUnit.SetTextColor(ForegroundColor);
            _usValue.SetTextSize(ComplexUnitType.Dip, fontSize);
            _usValue.SetTextColor(ForegroundColor);
            _usUnit.SetTextSize(ComplexUnitType.Dip, fontSize);
            _usUnit.SetTextColor(ForegroundColor);

            // Listen for width updates on metric and imperial scale lines to update the combined scale line
            _metricScaleLine.PropertyChanged += ScaleLine_PropertyChanged;
            _usScaleLine.PropertyChanged += ScaleLine_PropertyChanged;

            // ===============================================================
            // First row - placeholder, numeric text, and units text
            // ===============================================================
            var firstRowLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
            };

            firstRowLayout.AddView(_metricScaleLine);
            firstRowLayout.AddView(_metricValue);
            firstRowLayout.AddView(_metricUnit);

            // ================================================================================
            // Second row - first metric tick line, placeholder, and second metric tick line
            // ================================================================================
            var secondRowLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
            };

            var tickWidth = CalculateScreenDimension(2);
            var tickHeight = CalculateScreenDimension(5);

            _firstMetricTickLine = new RectangleView(Context, tickWidth, tickHeight) { BackgroundColor = ForegroundColor };

            _metricWidthPlaceholder = new RectangleView(Context, _metricScaleLine.Width, 1);
            _secondMetricTickLine = new RectangleView(Context, tickWidth, tickHeight) { BackgroundColor = ForegroundColor };

            secondRowLayout.AddView(_firstMetricTickLine);
            secondRowLayout.AddView(_metricWidthPlaceholder);
            secondRowLayout.AddView(_secondMetricTickLine);

            // ==============================================================================================
            // Third row - filler segment at start of scale line, combined metric/imperial scale line
            // ==============================================================================================
            var thirdRowLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
            };

            _scaleLineStartSegment = new RectangleView(Context, Math.Round(tickWidth) * 2, scalelineHeight) { BackgroundColor = ForegroundColor };

            thirdRowLayout.AddView(_scaleLineStartSegment);
            thirdRowLayout.AddView(_combinedScaleLine);

            // ==============================================================================================
            // Fourth row - first imperial tick line, placeholder, second imperial tick line
            // ==============================================================================================
            var fourthRowLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
            };

            _firstUsTickLine = new RectangleView(Context, tickWidth, tickHeight) { BackgroundColor = ForegroundColor };
            _secondUsTickLine = new RectangleView(Context, tickWidth, tickHeight) { BackgroundColor = ForegroundColor };

            fourthRowLayout.AddView(_firstUsTickLine);
            fourthRowLayout.AddView(_usScaleLine);
            fourthRowLayout.AddView(_secondUsTickLine);

            // ==========================================================================
            // Fifth row - placeholder, imperial numeric text, imperial unit text
            // ==========================================================================
            var fifthRowLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
            };

            _usWidthPlaceholder = new RectangleView(Context, _usScaleLine.Width, 1);

            fifthRowLayout.AddView(_usWidthPlaceholder);
            fifthRowLayout.AddView(_usValue);
            fifthRowLayout.AddView(_usUnit);

            // Add all scalebar rows to the root layout
            _rootLayout.AddView(firstRowLayout);
            _rootLayout.AddView(secondRowLayout);
            _rootLayout.AddView(thirdRowLayout);
            _rootLayout.AddView(fourthRowLayout);
            _rootLayout.AddView(fifthRowLayout);

            // Add root layout to view
            AddView(_rootLayout);
            _rootLayout.RequestLayout();
        }

        private Color _foregroundColor = Color.Black;

        /// <summary>
        /// Gets or sets the color of the foreground elements of the <see cref="ScaleLine"/>.
        /// </summary>
        public Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;

                if (_metricScaleLine == null)
                {
                    return;
                }

                // Apply specified color to scalebar elements
                _combinedScaleLine.BackgroundColor = value;
                _metricUnit.SetTextColor(value);
                _metricValue.SetTextColor(value);
                _usUnit.SetTextColor(value);
                _usValue.SetTextColor(value);
                _firstMetricTickLine.BackgroundColor = value;
                _secondMetricTickLine.BackgroundColor = value;
                _firstUsTickLine.BackgroundColor = value;
                _secondUsTickLine.BackgroundColor = value;
                _scaleLineStartSegment.BackgroundColor = value;
            }
        }

        private void ScaleLine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Synchronize place holder widths with calculated imperial and metric line widths
            _metricWidthPlaceholder.Width = _metricScaleLine.Width;
            _usWidthPlaceholder.Width = _usScaleLine.Width;

            // Update the scale line to be the longer of the metric or imperial lines
            _combinedScaleLine.Width = _metricScaleLine.Width > _usScaleLine.Width ? _metricScaleLine.Width : _usScaleLine.Width;
        }

        private void SetVisibility(bool isVisible)
        {
            Visibility = isVisible ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            // Initialize dimensions of root layout
            MeasureChild(_rootLayout, widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

            // Calculate the ideal width and height for the view
            var desiredWidth = PaddingLeft + PaddingRight + _rootLayout.MeasuredWidth;
            var desiredHeight = PaddingTop + PaddingBottom + _rootLayout.MeasuredHeight;

            // Get the width and height of the view given any width and height constraints indicated by the width and height spec values
            var width = ResolveSize(desiredWidth, widthMeasureSpec);
            var height = ResolveSize(desiredHeight, heightMeasureSpec);
            SetMeasuredDimension(width, height);
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // Forward layout call to the root layout
            _rootLayout.Layout(PaddingLeft, PaddingTop, _rootLayout.MeasuredWidth + PaddingLeft, _rootLayout.MeasuredHeight + PaddingBottom);
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
        private float CalculateScreenDimension(float pixels, ComplexUnitType screenUnitType = ComplexUnitType.Dip)
        {
            return !DesignTime.IsDesignMode ?
                TypedValue.ApplyDimension(screenUnitType, pixels, GetDisplayMetrics()) : pixels;
        }
    }
}