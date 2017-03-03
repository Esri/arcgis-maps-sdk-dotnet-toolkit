using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that creates a table of content tree view from a <see cref="GeoView"/>.
    /// </summary>
    [TemplatePart(Name ="List", Type = typeof(ItemsControl))]
    public class TableOfContents : Control
    {
        private bool _isScaleSet = false;
        private bool _scaleChanged;


        /// <summary>
        /// Initializes a new instance of the <see cref="TableOfContents"/> class.
        /// </summary>
        public TableOfContents()
        {
            DefaultStyleKey = typeof(TableOfContents);
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RebuildList();
        }

        /// <summary>
        /// Gets or sets the view to show the Table of Contents for.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoviewProperty); }
            set { SetValue(GeoviewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoviewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(TableOfContents), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contents = (TableOfContents)d;
            contents.OnViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
        }

        private void OnViewChanged(GeoView oldView, GeoView newView)
        {
            if (oldView != null)
            {
                (oldView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
                oldView.LayerViewStateChanged -= GeoView_LayerViewStateChanged;
            }
            if (newView != null)
            {
                (newView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
                newView.LayerViewStateChanged += GeoView_LayerViewStateChanged;
                newView.NavigationCompleted += GeoView_NavigationCompleted;
                _scaleChanged = true;
                _isScaleSet = false;
            }
            RebuildList();
        }

        private void GeoView_NavigationCompleted(object sender, EventArgs e)
        {
            UpdateScaleVisiblity();
        }

        private void UpdateScaleVisiblity()
        {
            if (_scaleChanged)
            {
                var scale = (GeoView as MapView)?.MapScale;
                if (scale.HasValue && layerContentList != null)
                {
                    _isScaleSet = true;
                    foreach (var item in layerContentList)
                    {
                        item.UpdateScaleVisibility(scale.Value, true);
                    }
                    _scaleChanged = false;
                }
            }
        }

        private void GeoView_LayerViewStateChanged(object sender, Esri.ArcGISRuntime.Mapping.LayerViewStateChangedEventArgs e)
        {
            if (layerContentList != null)
            {
                var l = layerContentList.FirstOrDefault(t => t.LayerContent == e.Layer);
                if (l != null)
                {
                    l.UpdateLayerViewState(e.LayerViewState);
                }
            }
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView && e.PropertyName == nameof(MapView.Map))
            {
                MapView mv = (MapView)sender;
                if(mv.Map != null)
                {
                    var incc = mv.Map as INotifyPropertyChanged;
                    var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(incc);
                    listener.OnEventAction = (instance, source, eventArgs) => { Map_PropertyChanged(source, eventArgs); };
                    listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                    incc.PropertyChanged += listener.OnEvent;
                }
                RebuildList();
            }
            else if (sender is SceneView && e.PropertyName == nameof(SceneView.Scene))
            {
                RebuildList();
            }
            else if (e.PropertyName == nameof(MapView.MapScale))
            {
                _scaleChanged = true;
                if (!_isScaleSet) //First time map is loaded and scale is established
                {
                    UpdateScaleVisiblity();
                }
            }
        }

        private void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.AllLayers))
            {
                RebuildList();
            }
        }

        private static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof(Object), typeof(TableOfContents), new PropertyMetadata(null, OnDocumentPropertyChanged));

        private static void OnDocumentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TableOfContents)d).RebuildList();
        }
        
        private ObservableLayerContentList layerContentList;

        private void RebuildList()
        {
            var list = GetTemplateChild("List") as ItemsControl;
            if (list != null)
            {
                if (GeoView != null)
                {
                    ObservableLayerContentList layers = null;
                    if (GeoView is MapView)
                    {
                        var map = (GeoView as MapView).Map;
                        if (map != null)
                        {
                            layers = new ObservableLayerContentList(GeoView as MapView, ShowLegend);
                        }
                        System.Windows.Data.Binding b = new System.Windows.Data.Binding();
                        b.Source = GeoView;
                        b.Path = new PropertyPath(nameof(MapView.Map));
                        b.Mode = System.Windows.Data.BindingMode.OneWay;
                        SetBinding(DocumentProperty, b);
                    }
                    else if (GeoView is SceneView)
                    {
                        var scene = (GeoView as SceneView).Scene;
                        if(scene != null)
                            layers = new ObservableLayerContentList(GeoView as SceneView, ShowLegend);
                        System.Windows.Data.Binding b = new System.Windows.Data.Binding();
                        b.Source = GeoView;
                        b.Path = new PropertyPath(nameof(SceneView.Scene));
                        b.Mode = System.Windows.Data.BindingMode.OneWay;
                        SetBinding(DocumentProperty, b);
                    }
                    if (layers != null && GeoView != null)
                    {
                        foreach (var l in layers)
                        {
                            var layer = l.LayerContent as Layer;
                            if (layer.LoadStatus == LoadStatus.Loaded)
                            {
                                l.UpdateLayerViewState(GeoView.GetLayerViewState(layer));
                            }
                        }
                        _scaleChanged = true;
                        UpdateScaleVisiblity();
                    }
                    list.ItemsSource = layerContentList = layers;
                }
                else
                {
                    list.ItemsSource = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the item template for each layer content entry
        /// </summary>
        public DataTemplate LayerItemTemplate
        {
            get { return (DataTemplate)GetValue(LayerItemTemplateProperty); }
            set { SetValue(LayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LayerItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerItemTemplateProperty =
            DependencyProperty.Register("LayerItemTemplate", typeof(DataTemplate), typeof(TableOfContents), new PropertyMetadata(null));
        
        /// <summary>
        /// Gets or sets whether to show a legend for the layers in the tree view
        /// </summary>
        public bool ShowLegend
        {
            get { return (bool)GetValue(ShowLegendProperty); }
            set { SetValue(ShowLegendProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowLegend"/> dependency property.
        /// </summary>        
        public static readonly DependencyProperty ShowLegendProperty =
            DependencyProperty.Register(nameof(ShowLegend), typeof(bool), typeof(TableOfContents), new PropertyMetadata(true, OnShowLegendPropertyChanged));

        private static void OnShowLegendPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TableOfContents)d).RebuildList();
        }
    }
}
