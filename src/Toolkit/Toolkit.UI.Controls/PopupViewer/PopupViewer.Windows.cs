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

#if WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class PopupViewer
    {
        private void Initialize() => DefaultStyleKey = GetType();

        /// <inheritdoc/>
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            Refresh();
        }

        private async void Refresh()
        {
            try
            {
                if (PopupManager != null)
                {
                    var expressions = await PopupManager.EvaluateExpressionsAsync();
                    var elements = PopupManager.Popup.EvaluatedElements;

                }
            }
            catch 
            {
            }
        }

        /// <summary>
        /// Gets or sets the associated PopupManager which contains popup and sketch editor.
        /// </summary>
        private PopupManager? PopupManagerImpl
        {
            get { return GetValue(PopupManagerProperty) as PopupManager; }
            set { SetValue(PopupManagerProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PopupManager"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PopupManagerProperty =
            DependencyProperty.Register(nameof(PopupManager), typeof(PopupManager), typeof(PopupViewer),
                new PropertyMetadata(null, OnPopupManagerPropertyChanged));

        private static void OnPopupManagerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PopupViewer)?.Refresh();
        }
    }
}
#endif