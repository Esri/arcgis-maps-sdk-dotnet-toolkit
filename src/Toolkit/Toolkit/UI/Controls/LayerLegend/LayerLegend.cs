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

using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The Legend Control that generates a list of Legend Items for a Layer.
    /// </summary>
    [System.Obsolete("Deprecated in favor of Legend control")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class LayerLegend : Control
    {
#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class.
        /// </summary>
        public LayerLegend()
            : base()
        {
            Initialize();
        }
#endif

        /// <summary>
        /// Gets or sets the layer to display the legend for.
        /// </summary>
        public ILayerContent LayerContent
        {
            get => LayerContentImpl;
            set => LayerContentImpl = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entire <see cref="ILayerContent"/> tree hierarchy should be rendered.
        /// </summary>
        public bool IncludeSublayers
        {
            get => IncludeSublayersImpl;
            set => IncludeSublayersImpl = value;
        }

        private async void LoadRecursive(IList<LegendInfo> itemsList, ILayerContent content, bool recursive)
        {
            if (content == null)
            {
                return;
            }

            try
            {
#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
                var legendInfo = await content.GetLegendInfosAsync();
#pragma warning restore ESRI1800
                if (legendInfo != null)
                {
                    foreach (var item in legendInfo)
                    {
                        itemsList.Add(item);
                    }
                }
            }
            catch
            {
                return;
            }

            if (recursive)
            {
                if (content.SublayerContents != null)
                {
                    foreach (var item in content.SublayerContents)
                    {
                        LoadRecursive(itemsList, item, recursive);
                    }
                }
            }
        }
    }
}
