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

using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.SymbolDisplay")]
    public partial class SymbolDisplay
    {
        private static DisplayMetrics s_displayMetrics;
        private static IWindowManager s_windowManager;
        private ImageView _imageView;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public SymbolDisplay(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public SymbolDisplay(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        private void Initialize()
        {
            _imageView = new ImageView(Context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };
            _imageView.SetMaxWidth(40);
            _imageView.SetMaxHeight(40);
            _imageView.SetScaleType(ImageView.ScaleType.CenterInside);

            AddView(_imageView);
        }

        private async Task UpdateSwatchAsync()
        {
            if (_imageView == null)
            {
                return;
            }

            if (Symbol == null)
            {
                _imageView.SetImageResource(0);
                _imageView.LayoutParameters.Width = 0;
                _imageView.LayoutParameters.Height = 0;
                return;
            }

#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
            try
            {
                var scale = GetScaleFactor();
                var imageData = await Symbol.CreateSwatchAsync(scale * 96);
                _imageView.LayoutParameters.Width = (int)(imageData.Width / scale);
                _imageView.LayoutParameters.Height = (int)(imageData.Height / scale);
                _imageView.SetImageBitmap(await imageData.ToImageSourceAsync());
            }
            catch
            {
                _imageView.SetImageResource(0);
            }
#pragma warning restore ESRI1800
        }

        private static double GetScaleFactor()
        {
            return GetDisplayMetrics()?.Density ?? 1;
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            // Initialize dimensions of root layout
            MeasureChild(_imageView, widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

            // Calculate the ideal width and height for the view
            var desiredWidth = PaddingLeft + PaddingRight + _imageView.MeasuredWidth;
            var desiredHeight = PaddingTop + PaddingBottom + _imageView.MeasuredHeight;

            // Get the width and height of the view given any width and height constraints indicated by the width and height spec values
            var width = ResolveSize(desiredWidth, widthMeasureSpec);
            var height = ResolveSize(desiredHeight, heightMeasureSpec);
            SetMeasuredDimension(width, height);
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // Forward layout call to the root layout
            _imageView.Layout(PaddingLeft, PaddingTop, _imageView.MeasuredWidth + PaddingLeft, _imageView.MeasuredHeight + PaddingBottom);
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
    }
}