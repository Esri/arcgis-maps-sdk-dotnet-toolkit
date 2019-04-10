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

#if NETFX_CORE
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal partial class TimeSliderRenderer
    {
        public TimeSliderRenderer()
        {
            Init();
            Element.SizeChanged += OnElementSizeChanged;
        }

        private void OnElementSizeChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        SizeRequest IVisualElementRenderer.GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            return new SizeRequest(new Size(DesiredSize.Width, DesiredSize.Height));
        }

        Windows.UI.Xaml.FrameworkElement IVisualElementRenderer.ContainerElement => this;

        Windows.UI.Xaml.UIElement IVisualElementRenderer.GetNativeElement() => this;
    }
}
#endif