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
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.ComponentModel;
using Xamarin.Forms;
#if __ANDROID__
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using Xamarin.Forms.Platform.iOS;
#elif NETFX_CORE
using Xamarin.Forms.Platform.UWP;
#endif

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.TimeSlider), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.TimeSliderRenderer))]
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal partial class TimeSliderRenderer : UI.Controls.TimeSlider, IVisualElementRenderer
    {
        private void Init()
        {
            CurrentExtentChanged += NativeTimeSlider_CurrentExtentChanged;
        }

        private void NativeTimeSlider_CurrentExtentChanged(object sender, UI.TimeExtentChangedEventArgs e)
        {
            Element?.RaiseCurrentExtentChanged(e);
        }

        #region IVisualElementRenderer
        VisualElement IVisualElementRenderer.Element => Element;

        private event EventHandler<VisualElementChangedEventArgs> _elementChanged;

        event EventHandler<VisualElementChangedEventArgs> IVisualElementRenderer.ElementChanged
        {
            add => _elementChanged += value;
            remove => _elementChanged -= value;
        }

        void IVisualElementRenderer.SetElement(VisualElement element) => SetElement(element);

        #endregion

        private void SetElement(Element element)
        {
            var oldElement = Element;
            Element = (TimeSlider)element;
            OnElementChanged(oldElement, Element);
            _elementChanged?.Invoke(this, new VisualElementChangedEventArgs(Element, oldElement));
        }

        public TimeSlider Element { get; private set; }

        private void OnElementChanged(TimeSlider oldElement, TimeSlider newElement)
        {
            if (oldElement != null)
            {
                oldElement.PropertyChanged -= OnElementPropertyChanged;
            }

            if (newElement != null)
            {
                newElement.PropertyChanged += OnElementPropertyChanged;

                CurrentExtent = Element.CurrentExtent;
                FullExtent = Element.FullExtent;
                TimeStepInterval = Element.TimeStepInterval;
            }
        }

        protected void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TimeSlider.CurrentExtent): CurrentExtent = Element.CurrentExtent; break;
                case nameof(TimeSlider.FullExtent): FullExtent = Element.FullExtent; break;
                case nameof(TimeSlider.TimeStepInterval): TimeStepInterval = Element.TimeStepInterval; break;
                default: break;
            }

            // TODO: Handle more properties
        }

        void IDisposable.Dispose()
        {
            if (Element != null)
            {
                SetElement(null);
            }
        }
    }
}
#endif