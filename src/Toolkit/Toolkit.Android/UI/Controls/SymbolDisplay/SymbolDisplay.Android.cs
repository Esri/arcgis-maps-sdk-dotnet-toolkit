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

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public SymbolDisplay(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public SymbolDisplay(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        private void Initialize()
        {
            SetScaleType(ImageView.ScaleType.Center);
        }

        private async Task UpdateSwatchAsync()
        {
            if (Symbol == null)
            {
                SetImageResource(0);
                return;
            }

#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
            try
            {
                var scale = GetScaleFactor(Context);
                var imageData = await Symbol.CreateSwatchAsync(scale * 96);
                SetImageBitmap(await imageData.ToImageSourceAsync());
                SourceUpdated?.Invoke(this, System.EventArgs.Empty);
            }
            catch
            {
                SetImageResource(0);
            }
#pragma warning restore ESRI1800
        }

        private static double GetScaleFactor(Context context)
        {
            return GetDisplayMetrics(context)?.Density ?? 1;
        }

        // Gets a display metrics object for calculating display dimensions
        private static DisplayMetrics GetDisplayMetrics(Context context)
        {
            if (s_displayMetrics == null)
            {
                if (s_windowManager == null)
                {
                    s_windowManager = context.GetSystemService(Context.WindowService)?.JavaCast<IWindowManager>();
                }

                if (s_windowManager == null)
                {
                    s_displayMetrics = context.Resources?.DisplayMetrics;
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