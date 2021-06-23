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

#pragma warning disable CS0618 // Type or member 'LayerLegend' is obsolete
[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.LayerLegend), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.LayerLegendRenderer))]
#pragma warning restore CS0618 // Type or member is obsolete

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    [System.Obsolete("Deprecated in favor of Legend control")]
    internal class LayerLegendRenderer : ViewRenderer<LayerLegend, UI.Controls.LayerLegend?>
    {
#if __ANDROID__
        public LayerLegendRenderer(Android.Content.Context context)
            : base(context)
        {
        }
#endif

        protected override void OnElementChanged(ElementChangedEventArgs<LayerLegend> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (Control == null)
                {
#if __ANDROID__
                    UI.Controls.LayerLegend ctrl = new UI.Controls.LayerLegend(Context);
#else
                    UI.Controls.LayerLegend ctrl = new UI.Controls.LayerLegend();
#endif
                    ctrl.IncludeSublayers = Element.IncludeSublayers;
                    ctrl.LayerContent = Element.LayerContent;
                    SetNativeControl(ctrl);
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Control != null)
            {
                if (e.PropertyName == LayerLegend.IncludeSublayersProperty.PropertyName)
                {
                    Control.IncludeSublayers = Element.IncludeSublayers;
                }
                else if (e.PropertyName == LayerLegend.LayerContentProperty.PropertyName)
                {
                    Control.LayerContent = Element.LayerContent;
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }
    }
}

#endif