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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Encapsulates a utility network trace operation, with all parameters and results, packaged for convenient use in a UI.
    /// </summary>
    internal class UtilityTraceOperationResult : INotifyPropertyChanged
    {
        private bool _areFeaturesSelected = true;
        private bool _areGraphicsShown;
        private System.Drawing.Color _visualizationColor = System.Drawing.Color.Blue;

        /// <summary>
        /// Gets the full results, if any, from the utility trace operation.
        /// </summary>
        public List<UtilityTraceResult> RawResults { get; } = new List<UtilityTraceResult>();

        /// <summary>
        /// Gets the function results, if any, from the utility trace operation.
        /// </summary>
        public List<UtilityTraceFunctionOutput> FunctionResults { get; } = new List<UtilityTraceFunctionOutput>();

        /// <summary>
        /// Gets the warnings, if any, from the utility trace operation.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Gets the graphics results, if any, from the utility trace operation.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="AreGraphicsShown"/> or <see cref="VisualizationState"/> will add or remove these graphics from <see cref="ResultOverlay"/>.
        /// </remarks>
        public List<Graphic> Graphics { get; } = new List<Graphic>();

        /// <summary>
        /// Gets the element results, if any, from the utility trace operation.
        /// </summary>
        public List<UtilityElement> ElementResults { get; } = new List<UtilityElement>();

        /// <summary>
        /// Gets the graphics overlay for visualizing graphics results. This will be populated with the graphics in <see cref="Graphics"/> by default.
        /// </summary>
        public GraphicsOverlay ResultOverlay { get; } = new GraphicsOverlay() { Opacity = 0.5 };

        /// <summary>
        /// Gets the utility element results, if any, grouped by asset group.
        /// </summary>
        public List<Tuple<UtilityAssetGroup, int>> ElementResultsGrouped { get; } = new List<Tuple<UtilityAssetGroup, int>>();

        /// <summary>
        /// Gets or sets the name for this trace operation. This is use configurable to facilitate differentiation of trace operations.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets the named trace configuration used to create this result.
        /// </summary>
        public UtilityNamedTraceConfiguration Configuration { get; }

        /// <summary>
        /// Gets the starting point inputs to the trace.
        /// </summary>
        internal IList<StartingPointModel> StartingPoints { get; }

        /// <summary>
        /// Gets teh parameters used to create this result.
        /// </summary>
        public UtilityTraceParameters Parameters { get; }

        /// <summary>
        /// Gets the features from the element results, if any.
        /// </summary>
        public List<ArcGISFeature> Features { get; } = new List<ArcGISFeature>();

        /// <summary>
        /// Gets or sets the error part of the trace result, if there is one.
        /// </summary>
        public Exception? Error { get; set; }

        private UtilityNetworkTraceToolController _controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityTraceOperationResult"/> class.
        /// </summary>
        internal UtilityTraceOperationResult(UtilityNetworkTraceToolController controller, UtilityNamedTraceConfiguration configuration, UtilityTraceParameters parameters, IList<StartingPointModel> startingPoints)
        {
            _controller = controller;
            Configuration = configuration;
            Parameters = parameters;
            StartingPoints = startingPoints;
            AreGraphicsShown = true;
            ZoomToCommand = new DelegateCommand(HandleZoomToResultCommand);
            DeleteCommand = new DelegateCommand(HandleDeleteCommand);
        }

        /// <summary>
        /// Gets or sets the color used for graphic symbology.
        /// </summary>
        /// <remarks>
        /// Only <see cref="SimpleFillSymbol"/>, <see cref="SimpleMarkerSymbol"/>, and <see cref="SimpleLineSymbol"/> will be affected when setting this property.
        /// </remarks>
        public System.Drawing.Color GraphicVisualizationColor
        {
            get => _visualizationColor;
            set
            {
                if (value != _visualizationColor)
                {
                    _visualizationColor = value;
                    OnPropertyChanged();
                    foreach (var graphic in ResultOverlay.Graphics)
                    {
                        if (graphic.Symbol is SimpleMarkerSymbol marker)
                        {
                            marker.Color = _visualizationColor;
                        }
                        else if (graphic.Symbol is SimpleLineSymbol lineSymbol)
                        {
                            lineSymbol.Color = _visualizationColor;
                        }
                        else if (graphic.Symbol is SimpleFillSymbol fillSymbol)
                        {
                            // Outline symbol must be updated first for the change to be picked up by SymbolDisplay
                            if (fillSymbol.Outline is SimpleLineSymbol outlineSymbol)
                            {
                                outlineSymbol.Color = _visualizationColor;
                            }

                            fillSymbol.Color = _visualizationColor;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether feature results are selected. Setting this value will select or unselect features in their respective feature layers.
        /// </summary>
        public bool AreFeaturesSelected
        {
            get => _areFeaturesSelected;
            set
            {
                if (value != _areFeaturesSelected)
                {
                    _areFeaturesSelected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VisualizationState));
                }

                if (Features != null)
                {
                    var groups = Features.OfType<ArcGISFeature>().GroupBy(candidate => candidate.FeatureTable?.Layer);
                    foreach (var grouping in groups)
                    {
                        if (grouping.Key is FeatureLayer featureLayer)
                        {
                            if (_areFeaturesSelected)
                            {
                                featureLayer.SelectFeatures(grouping);
                            }
                            else
                            {
                                featureLayer.UnselectFeatures(grouping);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether graphic results are shown. Setting this value will add or remove <see cref="Graphics"/> from <see cref="ResultOverlay"/>.
        /// </summary>
        public bool AreGraphicsShown
        {
            get => _areGraphicsShown;
            set
            {
                if (value != _areGraphicsShown)
                {
                    _areGraphicsShown = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VisualizationState));
                    ResultOverlay.Graphics.Clear();
                    if (_areGraphicsShown && Graphics.Any())
                    {
                        ResultOverlay.Graphics.AddRange(Graphics);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a composite value reflecting the combined values of <see cref="AreGraphicsShown"/> and <see cref="AreFeaturesSelected"/>.
        /// </summary>
        public bool? VisualizationState
        {
            get
            {
                if (AreGraphicsShown && AreFeaturesSelected)
                {
                    return true;
                }
                else if (!AreGraphicsShown && !AreFeaturesSelected)
                {
                    return false;
                }

                return null;
            }

            set
            {
                if (value.HasValue)
                {
                    AreFeaturesSelected = AreGraphicsShown = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ZoomToCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        private void HandleDeleteCommand(object? parameter)
        {
            _controller.Results.Remove(this);
        }

        private void HandleZoomToResultCommand(object? parameter)
        {
            if (parameter is UtilityTraceOperationResult targetResult)
            {
                var graphicsExtent = targetResult.ResultOverlay.Extent;
                var featureExtent = (targetResult.Features?.Any() == true) ? GeometryEngine.CombineExtents(targetResult.Features.Select(m => m.Geometry).OfType<Geometry.Geometry>()) : null;
                if (targetResult.ResultOverlay.Graphics.Any() && featureExtent != null && graphicsExtent != null && !graphicsExtent.IsEmpty)
                {
                    _controller.RequestedViewpoint = new Viewpoint(GeometryEngine.CombineExtents(graphicsExtent, featureExtent));
                }
                else if (featureExtent != null)
                {
                    _controller.RequestedViewpoint = new Viewpoint(featureExtent);
                }
                else if (targetResult.ResultOverlay.Graphics.Any() && graphicsExtent != null && !graphicsExtent.IsEmpty)
                {
                    _controller.RequestedViewpoint = new Viewpoint(graphicsExtent);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are any results from the operation.
        /// </summary>
        public bool HasAnyResults
        {
            get => Graphics.Any() || ElementResults.Any() || FunctionResults.Any();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}