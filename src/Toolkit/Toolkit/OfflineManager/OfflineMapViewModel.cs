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

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks.Offline;

// Swift reference: https://github.com/Esri/arcgis-maps-sdk-swift-toolkit/blob/main/Sources/ArcGISToolkit/Components/Offline/OfflineMapViewModel.swift

namespace Esri.ArcGISRuntime.Toolkit;


internal sealed class OfflineMapViewModel
{
    private OfflineMapInfo? _info;
    private OfflineMapTask _offlineMapTask;
    private PortalItem _portalItem;

    private OfflineMapViewModel(PortalItem item, OfflineMapTask offlineMapTask)
    {
        _portalItem = item;
        _offlineMapTask = offlineMapTask;
        LoadPreplanned();
        LoadOnDemand();
    }

    private void LoadOnDemand()
    {
        // OfflineManager.Default.OfflineMapInfos....
    }

    private async void LoadPreplanned()
    {
        PreplannedMapAreas.Clear();
        var preplanned = await _offlineMapTask.GetPreplannedMapAreasAsync();
        foreach (var area in preplanned)
        {
            try
            {
                await area.LoadAsync();
                PreplannedMapAreas.Add(area);
            }
            catch { }
        }
    }

    public ObservableCollection<PreplannedMapArea> PreplannedMapAreas { get; } = new ObservableCollection<PreplannedMapArea>();

    /// <summary>
    /// Creates an offline map areas view model for a given web map.
    /// </summary>
    /// <param name="onlineMap"></param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentException"></exception>
    public static async Task<OfflineMapViewModel> CreateAsync(Map onlineMap)
    {
        if (onlineMap.Item is not PortalItem item || string.IsNullOrEmpty(item.ItemId))
        {
            throw new System.ArgumentException("The provided map does not have a valid portal item of type WebMap.");
        }
        var offlineMapTask = await OfflineMapTask.CreateAsync(onlineMap);
        //_portalItem = ;
        //_info = info;
        OfflineMapViewModel model = new OfflineMapViewModel(item, offlineMapTask);
        
        return model;
    }


}