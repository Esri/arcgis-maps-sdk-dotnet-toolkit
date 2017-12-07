// /*******************************************************************************
//  * Copyright 2017 Esri
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
using Android.Widget;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass")]
    public partial class Compass
    {
        private class NorthArrowShape : View
        {
            public NorthArrowShape(Context context) : base(context) { }


            /// <inheritdoc />
            protected override void OnDraw(Canvas canvas)
            {
                // RectF space = new RectF(this.Left, this.Top, this.Right, this.Bottom);
                Paint paint = new Paint(PaintFlags.AntiAlias);
                paint.SetShader(new LinearGradient(0, 0, ArrowWidth, 0, Color.DarkRed, Color.Red, Shader.TileMode.Mirror));
                var path = new Path();
                path.MoveTo(ArrowWidth / 2, 0);
                path.LineTo(ArrowWidth / 4 * 3, ArrowHeight / 2);
                path.LineTo(ArrowWidth / 4, ArrowHeight / 2);
                path.Close();
                canvas.DrawPath(path, paint);

                paint.SetShader(new LinearGradient(0, 0, ArrowWidth, 0, Color.Gray, Color.LightGray, Shader.TileMode.Mirror));
                path = new Path();
                path.MoveTo(ArrowWidth / 4, ArrowHeight / 2);
                path.LineTo(ArrowWidth / 4 * 3, ArrowHeight / 2);
                path.LineTo(ArrowWidth / 2, ArrowHeight);
                path.Close();
                canvas.DrawPath(path, paint);

                base.OnDraw(canvas);
            }

            public int ArrowWidth { get; set; } = 40;
            public int ArrowHeight { get; set; } = 40;
        }

        private static DisplayMetrics s_displayMetrics;
        private static IWindowManager s_windowManager;
        private RelativeLayout _rootLayout;
        private NorthArrowShape _northArrow;

        /// <summary>
        /// Initializes a new instance of the <see cref="Compass"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public Compass(Context context) : base(context) { Initialize(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="Compass"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public Compass(Context context, IAttributeSet attr) : base(context, attr) { Initialize(); }

        private void Initialize()
        {
            // Vertically-oriented layout for containing all scalebar components
            _rootLayout = new RelativeLayout(Context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };
            _rootLayout.SetGravity(GravityFlags.Top);
            // TODO
            Android.Graphics.Drawables.GradientDrawable drawable = new Android.Graphics.Drawables.GradientDrawable();
            drawable.SetColor(new Color(255,255,255,128));
            drawable.SetShape(Android.Graphics.Drawables.ShapeType.Oval);
            drawable.SetStroke((int)CalculateScreenDimension(2), Color.Gray);
            var size = (int)CalculateScreenDimension(40);
            drawable.SetSize(size, size);
            ImageView iv = new ImageView(this.Context);
            iv.SetImageDrawable(drawable);
            _rootLayout.AddView(iv);

            _northArrow = new NorthArrowShape(this.Context) { ArrowHeight = size, ArrowWidth = size, PivotX = size / 2, PivotY = size / 2 };
            _rootLayout.AddView(_northArrow);

            // Add root layout to view
            AddView(_rootLayout);
            _rootLayout.RequestLayout();
        }

        private void UpdateCompassRotation(bool transition)
        {
            //SetVisibility(!AutoHide || Heading != 0);
            _northArrow.Rotation = -(float)this.Heading;
            _rootLayout.RequestLayout();
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            // Initialize dimensions of root layout
            //MeasureChild(_rootLayout, widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));
            MeasureChild(_rootLayout, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(widthMeasureSpec), MeasureSpecMode.AtMost), MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

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

        private void SetVisibility(bool isVisible)
        {
            Visibility = isVisible ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
        }

        // Gets a display metrics object for calculating display dimensions
        private static DisplayMetrics GetDisplayMetrics()
        {
            if (s_displayMetrics == null)
            {
                if (s_windowManager == null)
                    s_windowManager = Application.Context?.GetSystemService(Context.WindowService)?.JavaCast<IWindowManager>();
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