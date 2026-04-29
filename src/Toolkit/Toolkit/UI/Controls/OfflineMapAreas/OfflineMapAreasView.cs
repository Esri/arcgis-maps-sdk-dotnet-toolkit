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

using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;


#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using DependencyObject = Microsoft.Maui.Controls.BindableObject;
using ScrollViewer = Microsoft.Maui.Controls.ScrollView;
using BaseItemsControl = Microsoft.Maui.Controls.ItemsView;
#elif WPF
using BaseItemsControl = System.Windows.Controls.ItemsControl;
using Esri.ArcGISRuntime.Toolkit.Primitives;
#elif WINDOWS_XAML
using BaseItemsControl = Microsoft.UI.Xaml.Controls.ItemsControl;
using Esri.ArcGISRuntime.Toolkit.Primitives;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    public partial class OfflineMapAreasView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureFormView"/> class.
        /// </summary>
        public OfflineMapAreasView()
            : base()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(OfflineMapAreasView);
#endif
            // Since we're binding to a static collection, we need to unbind when the control is unloaded to prevent memory leaks.
            this.Unloaded += (s, e) => UnassignItemsSource();
            this.Loaded += (s, e) => AssignItemsSource();
        }


        private void AssignItemsSource()
        {
            if (GetTemplateChild(ItemsViewName) is BaseItemsControl lv)
            {
                lv.ItemsSource = OfflineManager.Instance.OfflineMapInfos;
            }
        }

        private void UnassignItemsSource()
        {
            if (GetTemplateChild(ItemsViewName) is BaseItemsControl lv)
            {
                lv.ItemsSource = null;
            }
        }

        /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            AssignItemsSource();
        }


        /// <summary>
        /// Gets or sets Online map to display areas for in the list.
        /// </summary>
        public Map? OnlineMap
        {
            get => GetValue(OnlineMapProperty) as Map;
            set => SetValue(OnlineMapProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OnlineMap"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty OnlineMapProperty =
            BindableProperty.Create(nameof(OnlineMap), typeof(Map), typeof(OfflineMapAreasView), propertyChanged: (s, oldValue, newValue) => ((OfflineMapAreasView)s).OnOnlineMapPropertyChanged(oldValue as Map, newValue as Map));
#else
        public static readonly DependencyProperty OnlineMapProperty =
            DependencyProperty.Register(nameof(OnlineMap), typeof(Map), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s, d) => ((OfflineMapAreasView)s).OnOnlineMapPropertyChanged(d.OldValue as Map, d.NewValue as Map)));
#endif

        private void OnOnlineMapPropertyChanged(Map? oldMap, Map? newMap)
        {
            if (newMap is not null && OfflineMapInfo is not null)
            {
                OfflineMapInfo = null; // Only one of OnlineMap or OfflineMapInfo can be set at a time, so clear the other when one is set.
            }
            if (newMap is not null)
            {
                SelectedMap = newMap;
            }
        }

        /// <summary>
        /// Gets or sets OfflineMapInfo to display areas for in the list.
        /// </summary>
        public OfflineMapInfo? OfflineMapInfo
        {
            get => GetValue(OfflineMapInfoProperty) as OfflineMapInfo;
            set => SetValue(OfflineMapInfoProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OfflineMapInfo"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty OfflineMapInfoProperty =
            BindableProperty.Create(nameof(OfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), propertyChanged: (s, oldValue, newValue) => ((OfflineMapAreasView)s).OnOfflineMapInfoPropertyChanged(oldValue as OfflineMapInfo, newValue as OfflineMapInfo));
#else
        public static readonly DependencyProperty OfflineMapInfoProperty =
            DependencyProperty.Register(nameof(OfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s, d) => ((OfflineMapAreasView)s).OnOfflineMapInfoPropertyChanged(d.OldValue as OfflineMapInfo, d.NewValue as OfflineMapInfo)));
#endif

        private void OnOfflineMapInfoPropertyChanged(OfflineMapInfo? oldMap, OfflineMapInfo? newOfflineMap)
        {
            if(newOfflineMap is not null && OnlineMap is not null)
            {
                OnlineMap = null; // Only one of OnlineMap or OfflineMapInfo can be set at a time, so clear the other when one is set.
            }
            if(newOfflineMap is not null)
            {
                SelectedOfflineMapInfo = newOfflineMap;
            }
        }

        /// <summary>
        /// Gets or sets item template for the <see cref="SelectedOfflineMapInfo"/> items in the list.
        /// </summary>
        public OfflineMapInfo? SelectedOfflineMapInfo
        {
            get => GetValue(SelectedOfflineMapInfoProperty) as OfflineMapInfo;
            set => SetValue(SelectedOfflineMapInfoProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedOfflineMapInfo"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty SelectedOfflineMapInfoProperty =
            BindableProperty.Create(nameof(SelectedOfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), propertyChanged: (s, oldValue, newValue) => ((OfflineMapAreasView)s).OnSelectedOfflineMapInfoPropertyChanged(oldValue as OfflineMapInfo, newValue as OfflineMapInfo));
#else
        public static readonly DependencyProperty SelectedOfflineMapInfoProperty =
            DependencyProperty.Register(nameof(SelectedOfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s, d) => ((OfflineMapAreasView)s).OnSelectedOfflineMapInfoPropertyChanged(d.OldValue as OfflineMapInfo, d.NewValue as OfflineMapInfo)));
#endif

        private void OnSelectedOfflineMapInfoPropertyChanged(OfflineMapInfo? oldMap, OfflineMapInfo? newOfflineMap)
        {
            
        }


#if MAUI
        private Map? _selectedMap;

        public Map? SelectedMap
        {
            get => _selectedMap;
            private set
            {
                if (_selectedMap != value) {
                    _selectedMap = value;
                    OnPropertyChanged(nameof(SelectedMap));
                }
            }
        }
#elif WINDOWS_XAML
        public Map? SelectedMap
        {
            get => GetValue(SelectedMapProperty) as Map; 
            private set => SetValue(SelectedMapProperty, value);
        }

        private static readonly DependencyProperty SelectedMapProperty =
            DependencyProperty.Register(nameof(SelectedMap), typeof(Map), typeof(OfflineMapAreasView), new PropertyMetadata(null));

#elif WPF
        public Map? SelectedMap
           {
            get => GetValue(SelectedMapPropertyKey.DependencyProperty) as Map; 
            private set => SetValue(SelectedMapPropertyKey, value);
        }

        private static readonly DependencyPropertyKey SelectedMapPropertyKey =
                DependencyProperty.RegisterReadOnly(
                  name: nameof(SelectedMap),
                  propertyType: typeof(Map),
                  ownerType: typeof(OfflineMapAreasView),
                  typeMetadata: new FrameworkPropertyMetadata());
#endif

        /// <summary>
        /// Gets or sets the vertical scrollbar visibility of the scrollviewer below the title.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VerticalScrollBarVisibility"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty VerticalScrollBarVisibilityProperty =
            BindableProperty.Create(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(OfflineMapAreasView), ScrollBarVisibility.Default);
#else
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(OfflineMapAreasView), new PropertyMetadata(ScrollBarVisibility.Auto));
#endif

        /// <summary>
        /// Gets or sets item template for the <see cref="OfflineMapInfo"/> items in the list.
        /// </summary>
        public DataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty) as DataTemplate;
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(OfflineMapAreasView), default(DataTemplate));
#else
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(OfflineMapAreasView), new PropertyMetadata(default(DataTemplate)));
#endif
    }
}
