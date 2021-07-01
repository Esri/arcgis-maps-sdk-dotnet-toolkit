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

#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;

[assembly: System.Runtime.CompilerServices.ReferenceAssembly]

// Ignore code-analyzer warnings to simplify the use of this class
#pragma warning disable SA1649
#pragma warning disable SA1402
#pragma warning disable SA1516

// This file contains a collection of stubs solely used for the .NET Standard build
// This is a little simpler than excluding all references to these classes
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class PopupViewer
    {
        public Mapping.Popups.PopupManager? PopupManager { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        internal void SetForeground(Color color) => throw new NotImplementedException();
    }

    internal class Compass
    {
        public bool AutoHide { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double Heading { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    internal class LayerLegend
    {
        public bool IncludeSublayers { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ILayerContent? LayerContent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    internal class Legend
    {
        public bool FilterByVisibleScaleRange { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public object? GeoView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ReverseLayerOrder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    internal class ScaleLine
    {
        public double MapScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double TargetWidth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        internal void SetForeground(Color color) => throw new NotImplementedException();
    }

    internal class SymbolDisplay
    {
        public Symbol? Symbol { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    internal class TimeSlider
    {
        public TimeExtent? CurrentExtent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeExtent? FullExtent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeValue? TimeStepInterval { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IReadOnlyList<DateTimeOffset>? TimeSteps { get => throw new NotImplementedException(); }
        public TimeSpan PlaybackInterval { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PlaybackDirection PlaybackDirection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public LoopMode PlaybackLoopMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsStartTimePinned { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsEndTimePinned { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsPlaying { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string FullExtentLabelFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CurrentExtentLabelFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string TimeStepIntervalLabelFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeSliderLabelMode LabelMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color ThumbStroke { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color ThumbFill { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color CurrentExtentFill { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color FullExtentFill { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color FullExtentStroke { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color TimeStepIntervalTickFill { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color PlaybackButtonsFill { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color PlaybackButtonsStroke { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color FullExtentLabelColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color CurrentExtentLabelColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color TimeStepIntervalLabelColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public void InitializeTimeSteps(int count) => throw new NotImplementedException();
        public Task InitializeTimePropertiesAsync(GeoView geoView) => throw new NotImplementedException();
        public Task InitializeTimePropertiesAsync(ITimeAware timeAwareLayer) => throw new NotImplementedException();
        public bool StepForward(int timeSteps = 1) => throw new NotImplementedException();
        public bool StepBack(int timeSteps = 1) => throw new NotImplementedException();
        public event EventHandler<TimeExtentChangedEventArgs>? CurrentExtentChanged;
    }
}

#pragma warning restore SA1516
#pragma warning restore SA1402
#pragma warning restore SA1649

#endif