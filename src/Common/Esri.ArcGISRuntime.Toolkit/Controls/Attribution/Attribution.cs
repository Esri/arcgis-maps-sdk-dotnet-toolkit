// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Helpers;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Geometry;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows.Controls;
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// The Attribution Control displays Copyright information for Layers that have the ICopyright 
    /// Interface implemented.
    /// </summary>
    public class Attribution : Control
    {
        #region Constructor

        // Listen for layers DP changes
        private readonly DependencyPropertyChangedListeners<Attribution> _layerPropertyChangedListeners;

        // Observe recursively Layers Collection changed
        private readonly LayerCollectionRecursiveObserver _layerCollectionRecursiveObserver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Attribution"/> class.
        /// </summary>
        public Attribution()
        {
#if NETFX_CORE
            DefaultStyleKey = typeof(Attribution);
#endif
            _layerPropertyChangedListeners = new DependencyPropertyChangedListeners<Attribution>(this)
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };

            _layerCollectionRecursiveObserver = new LayerCollectionRecursiveObserver();
            _layerCollectionRecursiveObserver.LayerAdded += (s, e) => OnLayerAdded(e.Layer);
            _layerCollectionRecursiveObserver.LayerRemoved += (s, e) => OnLayerRemoved(e.Layer);
            _layerCollectionRecursiveObserver.LayerMoved += (s, e) => OnLayerMoved();
        }

#if !NETFX_CORE
        static Attribution()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Attribution),
                new FrameworkPropertyMetadata(typeof(Attribution)));
        }
#endif
        #endregion

        #region DependencyProperty Layers

        /// <summary>
        /// Gets or sets the layers to display attribution for.
        /// </summary>
        public IEnumerable<Layer> Layers
        {
            get { return (IEnumerable<Layer>)GetValue(LayersProperty); }
            set { SetValue(LayersProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Layers"/> Dependency Property.
        /// </summary>
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register("Layers", typeof(IEnumerable<Layer>), typeof(Attribution), new PropertyMetadata(null, OnLayersPropertyChanged));

        private static void OnLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Attribution)
                (d as Attribution).OnLayersPropertyChanged(e.NewValue as IEnumerable<Layer>);
        }

        private void OnLayersPropertyChanged(IEnumerable<Layer> newLayers)
        {
            // Stop Observing the previous collection
            _layerCollectionRecursiveObserver.StopObserving();

            // Start observing the new collection
            _layerCollectionRecursiveObserver.StartObserving(newLayers);
        }

        #endregion

        #region DependencyProperty Scale
        /// <summary>
        /// Gets or sets the scale for filtering layers by their visible scale range and for getting the copyrights that may depend on the scale.
        /// </summary>
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Scale"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(Attribution), new PropertyMetadata(double.NaN, OnScalePropertyChanged));

        private static void OnScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var attribution = d as Attribution;
            if (attribution != null)
                attribution.UpdateAttributionItems();
        }
        #endregion

        #region DependencyProperty Extent
        /// <summary>
        /// Gets or sets the extent for which the layer copyrights are displayed.
        /// </summary>
        /// <seealso cref="IQueryCopyright"/>
        public Envelope Extent
        {
            get { return (Envelope)GetValue(ExtentProperty); }
            set { SetValue(ExtentProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Extent"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtentProperty =
            DependencyProperty.Register("Extent", typeof(Envelope), typeof(Attribution), new PropertyMetadata(null, OnExtentPropertyChanged));

        private static void OnExtentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var attribution = d as Attribution;
            if (attribution != null)
                attribution.UpdateAttributionItems();
        }
        #endregion

        #region Items Dependency Property

        /// <summary>
        /// Gets the items to display in attribution control.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public IEnumerable<string> Items
        {
            get { return (IEnumerable<string>)GetValue(ItemsProperty); }
            internal set { SetValue(ItemsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Items"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(IEnumerable<string>), typeof(Attribution), new PropertyMetadata(null));
        
        #endregion

        #region Layer Event Handlers

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CopyrightText" || e.PropertyName == "IsVisible" || e.PropertyName == "Status")
                UpdateAttributionItems();
        }

        private void OnLayerAdded(Layer layer)
        {
            _layerPropertyChangedListeners.Attach(layer, "IsVisible");
            _layerPropertyChangedListeners.Attach(layer, "Status");
            layer.PropertyChanged += OnLayerPropertyChanged;
            UpdateAttributionItems();
        }

        private void OnLayerRemoved(Layer layer)
        {
            _layerPropertyChangedListeners.Detach(layer);
            layer.PropertyChanged -= OnLayerPropertyChanged;
            UpdateAttributionItems();
        }

        private void OnLayerMoved()
        {
            UpdateAttributionItems();
        }

        #endregion

        #region Private Methods

        private ThrottleTimer _updateItemsTimer;
        private void UpdateAttributionItems()
        {
            // wait for the map to stop navigating so map navigation performance doesn't suffer from it.
            // also useful to avoid too much updates during initialization
            if (_updateItemsTimer == null)
            {
                _updateItemsTimer = new ThrottleTimer(100) { Action = UpdateAttributionItemsImpl };
            }
            _updateItemsTimer.Invoke();
        }

        private void UpdateAttributionItemsImpl()
        {
            if (Layers == null)
                Items = null;
            else
            {
                string[] items = Layers.EnumerateLeaves(l => l.IsVisible && IsInScaleRange(l))
                                       .Select(CopyrightText).Where(cpr => !string.IsNullOrEmpty(cpr))
                                       .Select(cpr => cpr.Trim()).Distinct()
                                       .ToArray();
                if (!AreEquals(Items as string[], items)) // Avoid changing Items if the list didn't change
                    Items = items;
            }
        }

        private bool AreEquals(string[] strings1, string[] strings2)
        {
            return strings1 != null && strings2 != null && strings1.Length == strings2.Length && strings1.Zip(strings2, (string1, string2) => string1 == string2).All(b => b);
        }

        private bool IsInScaleRange(Layer layer)
        {
            return !(Scale > 0.0) || (!(layer.MinScale < Scale) && !(layer.MaxScale > Scale)); // Note: '!' useful for managing correctly NaN cases 
        }

        private string CopyrightText(Layer l)
        {
            if (l is IQueryCopyright)
            {
                return ((IQueryCopyright)l).QueryCopyright(Extent, Scale);
            }
            else if (l is ICopyright)
            {
                return ((ICopyright)l).CopyrightText;
            }
            else
                return null;
        }

        #endregion
    }
}
