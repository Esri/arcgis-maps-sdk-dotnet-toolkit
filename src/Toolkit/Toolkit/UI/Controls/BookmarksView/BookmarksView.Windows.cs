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

#if !XAMARIN
using System.Collections.Generic;
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "List", Type = typeof(ListView))]
    public partial class BookmarksView
    {
        private void Initialize() => DefaultStyleKey = typeof(BookmarksView);

        private void GeoDoc_PropertyChange(object sender, PropertyChangedEventArgs e)
        {
            ConfigureGeoDocEvents(GeoView);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            ListView = GetTemplateChild("List") as ListView;

            Refresh();
        }

        internal void Refresh()
        {
            if (ListView == null)
            {
                return;
            }

            ListView.ItemsSource = CurrentBookmarkList;
        }

        private ListView _listView;

        private ListView ListView
        {
            get => _listView;
            set
            {
                if (value != _listView)
                {
                    if (_listView != null)
                    {
                        _listView.SelectionChanged -= ListSelectionChanged;
                        _listView = value;
                    }

                    _listView = value;

                    if (_listView != null)
                    {
                        _listView.SelectionChanged += ListSelectionChanged;
                    }
                }
            }
        }

        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is Bookmark bm)
                {
                    SelectAndNavigateToBookmark(bm);
                }
            }

            ((ListView)sender).SelectedItem = null;
        }

        private GeoView GeoViewImpl
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        private IEnumerable<Bookmark> BookmarksOverrideImpl
        {
            get { return (IEnumerable<Bookmark>)GetValue(BookmarksOverrideProperty); }
            set { SetValue(BookmarksOverrideProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(BookmarksView), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="BookmarksOverride" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty BookmarksOverrideProperty =
            DependencyProperty.Register(nameof(BookmarksOverride), typeof(IList<Bookmark>), typeof(BookmarksView), new PropertyMetadata(null, OnBookmarksOverridePropertyChanged));

#if NETFX_CORE
        // Token used for unregistering GeoView Map/Scene property change callbacks
        private long _lasttoken;
#endif

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contents = (BookmarksView)d;
#if NETFX_CORE
            if (e.OldValue is MapView oldMapView)
            {
                oldMapView.UnregisterPropertyChangedCallback(MapView.MapProperty, contents._lasttoken);
            }
            else if (e.OldValue is SceneView oldSceneView)
            {
                oldSceneView.UnregisterPropertyChangedCallback(SceneView.SceneProperty, contents._lasttoken);
            }

            if (e.NewValue is MapView newMapView)
            {
                contents._lasttoken = newMapView.RegisterPropertyChangedCallback(MapView.MapProperty, contents.GeoView_PropertyChanged);
            }
            else if (e.NewValue is SceneView newSceneView)
            {
                contents._lasttoken = newSceneView.RegisterPropertyChangedCallback(SceneView.SceneProperty, contents.GeoView_PropertyChanged);
            }
#else
            if (e.OldValue is MapView oldMapView)
            {
                DependencyPropertyDescriptor
                    .FromProperty(MapView.MapProperty, typeof(MapView))
                    .RemoveValueChanged(oldMapView, contents.GeoView_PropertyChanged);
            }
            else if (e.OldValue is SceneView oldSceneView)
            {
                DependencyPropertyDescriptor
                    .FromProperty(SceneView.SceneProperty, typeof(SceneView))
                    .RemoveValueChanged(oldSceneView, contents.GeoView_PropertyChanged);
            }

            if (e.NewValue is MapView newMapView)
            {
                DependencyPropertyDescriptor
                .FromProperty(MapView.MapProperty, typeof(MapView))
                .AddValueChanged(newMapView, contents.GeoView_PropertyChanged);
            }
            else if (e.NewValue is SceneView newSceneView)
            {
                DependencyPropertyDescriptor
                .FromProperty(SceneView.SceneProperty, typeof(SceneView))
                .AddValueChanged(newSceneView, contents.GeoView_PropertyChanged);
            }
#endif
            contents.Refresh();
        }

#if NETFX_CORE
        private void GeoView_PropertyChanged(DependencyObject sender, DependencyProperty property)
        {
            ConfigureGeoDocEvents(GeoView);
        }
#else
        private void GeoView_PropertyChanged(object sender, System.EventArgs e)
        {
            ConfigureGeoDocEvents(GeoView);
        }
#endif

        private static void OnBookmarksOverridePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bm = (BookmarksView)d;
            bm.BookmarksOverride = (IEnumerable<Bookmark>)e.NewValue;
            bm.Refresh();
        }

        /// <summary>
        /// Gets or sets the item template used to render bookmark entries in the list.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(BookmarksView), new PropertyMetadata(null));
    }
}
#endif