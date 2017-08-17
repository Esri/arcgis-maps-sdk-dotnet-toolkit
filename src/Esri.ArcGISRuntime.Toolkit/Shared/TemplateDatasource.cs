// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    public class TemplateDataSource
    {
        private LayerCollectionMonitor<FeatureLayer> _featureLayers;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateDataSource"/> class.
        /// </summary>
        /// <param name="layers">A collection of layers to pull feature templates from.</param>
        public TemplateDataSource(IEnumerable<Layer> layers)
        {
            _featureLayers = new LayerCollectionMonitor<FeatureLayer>(layers);
            _featureLayers.CollectionChanged += FeatureLayers_CollectionChanged;
            _templates = new ObservableCollection<KeyValuePair<ArcGISFeatureTable, IList<FeatureTemplate>>>();
            Initialize();
        }

        private void Initialize()
        {
            _templates.Clear();
            foreach (var table in _featureLayers.Select(f => f.FeatureTable).OfType<ArcGISFeatureTable>())
            {
                KeyValuePair<ArcGISFeatureTable, IList<FeatureTemplate>> l = new KeyValuePair<ArcGISFeatureTable, IList<FeatureTemplate>>(table, new List<FeatureTemplate>());
                foreach (var item in table.FeatureTemplates)
                {
                    l.Value.Add(item);
                }

                foreach (var type in table.FeatureTypes)
                {
                    foreach (var item in type.Templates)
                    {
                        l.Value.Add(item);
                    }
                }
                _templates.Add(l);
            }
        }

        private void FeatureLayers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // if (e.Action == NotifyCollectionChangedAction.Reset)
                Initialize();

            // else {
            //     if (e.NewItems != null)
            //     {
            //         e.NewItems
            //     }
            // }
        }

        private ObservableCollection<KeyValuePair<ArcGISFeatureTable, IList<FeatureTemplate>>> _templates = new ObservableCollection<KeyValuePair<ArcGISFeatureTable, IList<FeatureTemplate>>>();

        /// <summary>
        /// Gets an observable read-only list of feature templates
        /// from all loaded <see cref="FeatureLayer"/>s in the provider list of layers.
        /// </summary>
        public IReadOnlyList<KeyValuePair<ArcGISFeatureTable, IList<FeatureTemplate>>> FeatureTemplates
        {
            get
            {
                return _templates;
            }
        }
    }
}
