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

using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.SymbolDisplay")]
    public partial class SymbolDisplay
    {
        private LinearLayout _rootLayout;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public SymbolDisplay(Context context) : base(context) { Initialize(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public SymbolDisplay(Context context, IAttributeSet attr) : base(context, attr) { Initialize(); }

        private void Initialize()
        {
            _rootLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };
            _rootLayout.SetGravity(GravityFlags.Top);

            // TODO

            AddView(_rootLayout);
            _rootLayout.RequestLayout();
        }

        private void Refresh()
        {
            // TODO
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
    }
}