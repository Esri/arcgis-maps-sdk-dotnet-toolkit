using System.ComponentModel;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Models a barrier used for a utility network trace.
    /// </summary>
    internal class BarrierModel : UtilityElementModel, INotifyPropertyChanged
    {
        private readonly UtilityNetworkTraceToolController _controller;
        private bool _useAsFilterBarrier;

        internal BarrierModel(UtilityNetworkTraceToolController controller, UtilityElement element, Graphic selectionGraphic, Feature feature, Envelope? zoomToExtent, bool? useAsFilterBarrier)
            : base(element, selectionGraphic, feature, zoomToExtent, model => controller.Barriers.Remove((BarrierModel)model))
        {
            _controller = controller;
            _useAsFilterBarrier = useAsFilterBarrier == true;
        }

        /// <summary>
        /// Gets the underlying utility element.
        /// </summary>
        public UtilityElement Barrier => Element;

        /// <summary>
        /// Gets or sets a value indicating whether the barrier is evaluated during the second trace pass.
        /// </summary>
        public bool UseAsFilterBarrier
        {
            get => _useAsFilterBarrier;
            set
            {
                if (_useAsFilterBarrier != value)
                {
                    _useAsFilterBarrier = value;
                    OnPropertyChanged();
                    _controller.HandleBarrierChanged();
                }
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}