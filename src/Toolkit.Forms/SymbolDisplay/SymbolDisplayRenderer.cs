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

#if !NETSTANDARD2_0
using System.ComponentModel;
using Xamarin.Forms;
#if __ANDROID__
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using Xamarin.Forms.Platform.iOS;
#elif NETFX_CORE
using Xamarin.Forms.Platform.UWP;
#endif

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.SymbolDisplay), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.SymbolDisplayRenderer))]

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class SymbolDisplayRenderer : ViewRenderer<SymbolDisplay, UI.Controls.SymbolDisplay?>
    {
#if __ANDROID__
        public SymbolDisplayRenderer(Android.Content.Context context)
            : base(context)
        {
        }
#endif

        protected override void OnElementChanged(ElementChangedEventArgs<SymbolDisplay> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (Control == null)
                {
#if __ANDROID__
                    UI.Controls.SymbolDisplay ctrl = new UI.Controls.SymbolDisplay(Context);
#else
                    UI.Controls.SymbolDisplay ctrl = new UI.Controls.SymbolDisplay();
#endif
                    ctrl.Symbol = Element.Symbol;
                    ctrl.SourceUpdated += (s, args) => Element?.InvalidateMeasure_Internal();
                    SetNativeControl(ctrl);
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SymbolDisplay.SymbolProperty.PropertyName && Control != null)
            {
                Control.Symbol = Element.Symbol;
            }

            base.OnElementPropertyChanged(sender, e);
        }
    }
}
#endif