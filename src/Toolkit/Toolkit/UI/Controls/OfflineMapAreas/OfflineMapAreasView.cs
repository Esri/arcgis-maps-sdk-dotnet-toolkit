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

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
#else
using Esri.ArcGISRuntime.Toolkit.Primitives;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// 
    /// </summary>
    public partial class OfflineMapAreasView
    {
        private Map? _onlineMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="OfflineMapAreasView"/> class.
        /// </summary>
        public OfflineMapAreasView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(OfflineMapAreasView);
#endif
        }

        /// <summary>
        /// Gets or sets the online map to take offline.
        /// </summary>
        public PortalItem? PortalItem
        {
            get => GetValue(PortalItemProperty) as PortalItem;
            set => SetValue(PortalItemProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PortalItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PortalItemProperty =
            PropertyHelper.CreateProperty<PortalItem, OfflineMapAreasView>(nameof(PortalItem), propertyChanged: (view,oldItem,newItem) => view.OnPortalItemChanged(newItem));

        private async void OnPortalItemChanged(PortalItem? newItem)
        {
            SelectedMap = null;
            _onlineMap = null;
            OfflineMapViewModel = null;
            if (newItem is not null)
            {
                _onlineMap = new Map(newItem);
                try
                {
                    OfflineMapViewModel = await OfflineMapViewModel.CreateAsync(_onlineMap);
                }
                catch {
                    //TODO: Trace.Write
                }
            }
        }

        private OfflineMapViewModel? _offlineMapViewModel;

        private OfflineMapViewModel? OfflineMapViewModel
        {
            get => _offlineMapViewModel;
            set
            {
                if (_offlineMapViewModel != value)
                {
                    //TODO: Clear any other state associated with this internal VM
                    _offlineMapViewModel = value;
                    if (GetTemplateChild("OfflineMapAreasListView") is ListView lv)
                    {
                        lv.ItemsSource = _offlineMapViewModel?.PreplannedMapAreas;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected map from list of available offline maps
        /// </summary>
        public Map? SelectedMap
        {
            get => GetValue(SelectedMapProperty) as Map;
            set => SetValue(SelectedMapProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedMap"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedMapProperty =
            PropertyHelper.CreateProperty<Map, OfflineMapAreasView>(nameof(SelectedMap), null);
    }
}