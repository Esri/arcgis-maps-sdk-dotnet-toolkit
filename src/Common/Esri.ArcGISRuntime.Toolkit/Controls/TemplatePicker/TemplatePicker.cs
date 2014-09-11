// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Diagnostics;
using System.Linq;
using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.Helpers;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{

    /// <summary>
    /// A template picker control enables selecting feature types to add
    /// when editing a feature layer.
    /// </summary>
    [TemplatePart(Name = "TemplateItems", Type = typeof(ItemsControl))]
    [StyleTypedProperty(Property = "ItemTemplate", StyleTargetType = typeof(FrameworkElement))]
    public class TemplatePicker : Control
    {
        // Underlying ItemsControl
        private ItemsControl _itemsControl;

        // TemplateItems by layer (useful to update quickly ItemsSource when a layer visibility changes)
        private readonly Dictionary<Layer, List<TemplateItem>> _templatesByLayer = new Dictionary<Layer, List<TemplateItem>>();

        // WeakEventListeners for feature layers changes (so the template picker may be released even if the feature layers are long lived object)

        // Listen for feature layers DP changes
        private readonly DependencyPropertyChangedListeners<TemplatePicker> _layerPropertyChangedListeners;

        // Observe recursively Layers Collection changed
        private readonly LayerCollectionRecursiveObserver _layerCollectionRecursiveObserver;

        private ThrottleTimer _updateItemsSourceTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatePicker"/> class.
        /// </summary>
        public TemplatePicker()
        {
            DefaultStyleKey = typeof(TemplatePicker);
            _layerPropertyChangedListeners = new DependencyPropertyChangedListeners<TemplatePicker>(this)
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };

            _layerCollectionRecursiveObserver = new LayerCollectionRecursiveObserver();
            _layerCollectionRecursiveObserver.LayerAdded += (s, e) => OnLayerAdded(e.Layer);
            _layerCollectionRecursiveObserver.LayerRemoved += (s, e) => OnLayerRemoved(e.Layer);
            _layerCollectionRecursiveObserver.LayerMoved += (s, e) => OnLayerMoved();
        }

        /// <summary>
        /// Occurs when a template is selected.
        /// </summary>
        public event EventHandler<TemplatePickedEventArgs> TemplatePicked;

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application 
        /// code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
#if NETFX_CORE
        protected 
#else 
        public
#endif
        override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _itemsControl = GetTemplateChild("TemplateItems") as ItemsControl;
            InitItemsSource();
        }

        private void OnItemClicked(object item)
        {
            var templateItem = item as TemplateItem;
            if (TemplatePicked != null && templateItem != null)
            {
                TemplatePicked(this, new TemplatePickedEventArgs(templateItem.Layer, templateItem.FeatureType, templateItem.FeatureTemplate));
            }
        }

        private void RebuildTemplate(FeatureLayer flayer)
        {
            var templates = new List<TemplateItem>();
            FeatureServiceLayerInfo serviceInfo = null;
            var ft = flayer.FeatureTable;
            if (ft != null && !ft.IsReadOnly && flayer.Status == LayerStatus.Initialized)
            {
                try
                {
                    serviceInfo = ft.ServiceInfo;
                }
                catch{}
            }
            if (serviceInfo != null)
            {
                var cmd = new InvokeCommand(OnItemClicked);
                var renderer = flayer.Renderer ?? (serviceInfo.DrawingInfo == null ? null : serviceInfo.DrawingInfo.Renderer);
                if (serviceInfo.Templates != null)
                {
                    foreach (FeatureTemplate template in serviceInfo.Templates) 
                    {
                        var item = new TemplateItem
                        {
                            Layer = flayer,
                            FeatureTemplate = template,
                            Command = cmd,
                        };
                        templates.Add(item);
                        if (renderer != null)
                        {
                            var g = new Graphic(template.Prototype.Attributes ?? Enumerable.Empty<System.Collections.Generic.KeyValuePair<string, object>>()); // Need to disambiguate from winstore toolkit KeyValuePair
                            item.SetSwatch(renderer.GetSymbol(g));
                        }
                    }
                }
                if (serviceInfo.Types != null)
                {
                    foreach (FeatureType type in serviceInfo.Types)
                    {
                        foreach (FeatureTemplate template in type.Templates)
                        {
                            var item = new TemplateItem
                            {
                                Layer = flayer,
                                FeatureType = type,
                                FeatureTemplate = template,
                                Command = cmd,
                            };
                            templates.Add(item);
                            if (renderer != null)
                            {
                                var g = new Graphic(template.Prototype.Attributes ?? Enumerable.Empty<System.Collections.Generic.KeyValuePair<string, object>>());
                                item.SetSwatch(renderer.GetSymbol(g));
                            }
                        }
                    }
                }
            }
            if (templates.Any())
                _templatesByLayer[flayer] = templates;
            else
                _templatesByLayer.Remove(flayer);
        }

        private void InitItemsSource()
        {
            // wait for the map to stop navigating so
            //map navigation performance doesn't suffer from it.
            if (_updateItemsSourceTimer == null)
            {
                _updateItemsSourceTimer = new ThrottleTimer(50) { Action = InitItemsSourceImpl };
            }
            _updateItemsSourceTimer.Invoke();
        }

        private void InitItemsSourceImpl()
        {
            if (_itemsControl != null)
            {
                var templates = new List<TemplateItem>();
                if (Layers != null)
                {
                    foreach (FeatureLayer flayer in Layers.EnumerateLeaves(l => l.IsVisible && IsInScaleRange(l)).OfType<FeatureLayer>())
                    {
                        if (_templatesByLayer.ContainsKey(flayer))
                            templates.AddRange(_templatesByLayer[flayer]);
                    }
                }
                if (!AreEquals(_itemsControl.ItemsSource as ICollection<TemplateItem>, templates)) // Avoid changing ItemSources if the list didn't change
                    _itemsControl.ItemsSource = templates;
            }
        }

        private bool AreEquals(ICollection<TemplateItem> templates1, ICollection<TemplateItem> templates2)
        {
            return templates1 != null && templates2 != null && templates1.Count == templates2.Count && templates1.Zip(templates2, (item1, item2) => item1 == item2).All(b => b);
        }

        private bool IsInScaleRange(Layer layer)
        {
            return !(Scale > 0.0) || (!(layer.MinScale < Scale) && !(layer.MaxScale > Scale)); // Note: ! useful for managing correctly NaN cases 
        }

        /// <summary>
        /// Gets or sets the layers for which templates are displayed.
        /// </summary>
        public IEnumerable<Layer> Layers
        {
            get { return (IEnumerable<Layer>)GetValue(LayersProperty); }
            set { SetValue(LayersProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Layers"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register("Layers", typeof(IEnumerable<Layer>), typeof(TemplatePicker), new PropertyMetadata(null, OnLayersPropertyChanged));

        private static void OnLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = d as TemplatePicker;
            if (picker != null)
                picker.OnLayersPropertyChanged(e.NewValue as IEnumerable<Layer>);
        }

        private void OnLayersPropertyChanged(IEnumerable<Layer> newLayers)
        {
            // Stop Observing the previous collection
            _layerCollectionRecursiveObserver.StopObserving();

            Debug.Assert(!_templatesByLayer.Any()); // here all layers should have been removed
            // Start observing the new collection
            _layerCollectionRecursiveObserver.StartObserving(newLayers);
        }


        private void OnLayerPropertyChanged(DependencyObject sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsVisible" || e.PropertyName == "MinScale" || e.PropertyName == "MaxScale")
                InitItemsSource();
            else // "Renderer"/"Status"/"FeatureTable"
            {
                var flayer = sender as FeatureLayer;
                Debug.Assert(flayer != null);
                if (flayer != null)
                {
                    RebuildTemplate(flayer);
                    InitItemsSource();
                }
            }
        }

        /// <summary>
        /// Gets or sets the data template used to display each TemplatePicker item.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(TemplatePicker), null);


        /// <summary>
        /// Gets or sets the template that defines the panel that controls the layout of items.
        /// </summary>
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemsPanel"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register("ItemsPanel", typeof(ItemsPanelTemplate), typeof(TemplatePicker), null);


        /// <summary>
        /// Gets or sets the scale if filtering layers by their visible scale range.
        /// </summary>
        /// <remarks>Typically this value has to be binded to the <see cref="MapView.Scale">MapView scale property</see></remarks>
        /// <value>
        /// The scale filter.
        /// </value>
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Scale"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(TemplatePicker), new PropertyMetadata(double.NaN, OnScalePropertyChanged));

        private static void OnScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = d as TemplatePicker;
            if (picker != null)
                picker.InitItemsSource();
        }


        private void OnLayerAdded(Layer layer)
        {
            _layerPropertyChangedListeners.Attach(layer, "IsVisible");
            _layerPropertyChangedListeners.Attach(layer, "MinScale");
            _layerPropertyChangedListeners.Attach(layer, "MaxScale");
            var flayer = layer as FeatureLayer;
            if (flayer != null)
            {
                _layerPropertyChangedListeners.Attach(flayer, "Renderer");
                _layerPropertyChangedListeners.Attach(flayer, "Status");
                _layerPropertyChangedListeners.Attach(flayer, "FeatureTable");
                RebuildTemplate(flayer);
                InitItemsSource();
            }
        }

        private void OnLayerRemoved(Layer layer)
        {
            _layerPropertyChangedListeners.Detach(layer);
            var flayer = layer as FeatureLayer;
            if (flayer != null && _templatesByLayer.ContainsKey(flayer))
            {
                _templatesByLayer.Remove(flayer);
                InitItemsSource();
            }
        }

        private void OnLayerMoved()
        {
            InitItemsSource();
        }


        private sealed class InvokeCommand : ICommand
        {
            private readonly Action<object> _onExecuted;
            public InvokeCommand(Action<object> onExecuted)
            {
                _onExecuted = onExecuted;
            }
            public bool CanExecute(object parameter)
            {
                return _onExecuted != null;
            }

#pragma warning disable 67 //Required by ICommand but not needed
            public event EventHandler CanExecuteChanged;
#pragma warning restore 67

            public void Execute(object parameter)
            {
                _onExecuted(parameter);
            }
        }

        private sealed class TemplateItem : INotifyPropertyChanged
        {
            public FeatureLayer Layer { get; set; }
            public FeatureType FeatureType { get; set; }
            public FeatureTemplate FeatureTemplate { get; set; }
            public ImageSource Swatch { get; set; }
            internal async void SetSwatch(Symbology.Symbol symbol)
            {
                if (symbol != null)
                {
                    // force the geometry type since GeometryType.Unknown doesn't work well with advanced symbology.
                    Geometry.GeometryType geometryType = Geometry.GeometryType.Unknown;
                    if (Layer != null && Layer.FeatureTable != null && Layer.FeatureTable.ServiceInfo != null)
                        geometryType = Layer.FeatureTable.ServiceInfo.GeometryType;

                    try
                    {
                        Swatch = await symbol.CreateSwatchAsync(32, 32, 96, Colors.Transparent, geometryType);
                        OnPropertyChanged("Swatch");
                    }
                    catch { }
                }
            }
            public ICommand Command { get; set; }
            private void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        /// <summary>
        /// EventArgs type for <see cref="TemplatePicker.TemplatePicked"/> event.
        /// </summary>
        public sealed class TemplatePickedEventArgs : EventArgs
        {
            internal TemplatePickedEventArgs(FeatureLayer layer,
                FeatureType featureType,
                FeatureTemplate featureTemplate)
            {
                Layer = layer;
                FeatureType = featureType;
                FeatureTemplate = featureTemplate;
            }

            /// <summary>
            /// Gets the feature layer of the selected template.
            /// </summary>
            /// <value>
            /// The faeture layer.
            /// </value>
            public FeatureLayer Layer { get; private set; }

            /// <summary>
            /// Gets the feature sub type selected. 
            /// </summary>
            /// <remarks>The sub type is null when the template is not associated to a sub type
            /// (for example when the templates are generated from an UniqueValueRenderer)</remarks>
            /// <value>
            /// The sub type of the template selected.
            /// </value>
            public FeatureType FeatureType { get; private set; }

            /// <summary>
            /// Gets the feature template selected.
            /// </summary>
            /// <value>sc
            /// The feature template.
            /// </value>
            public FeatureTemplate FeatureTemplate { get; private set; }
        }
    }
}
