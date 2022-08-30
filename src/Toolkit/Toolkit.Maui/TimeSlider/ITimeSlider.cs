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

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public interface ITimeSlider : IView
    {
        TimeExtent? CurrentExtent { get; set; }
        TimeExtent? FullExtent { get; set; }
        TimeValue? TimeStepInterval { get; set; }
        IReadOnlyList<DateTimeOffset>? TimeSteps { get; set; }
        TimeSpan PlaybackInterval { get; set; }
        PlaybackDirection PlaybackDirection { get; set; }
        LoopMode PlaybackLoopMode { get; set; }
        bool IsStartTimePinned { get; set; }
        bool IsEndTimePinned { get; set; }
        bool IsPlaying { get; set; }
        string? FullExtentLabelFormat { get; set; }
        string? CurrentExtentLabelFormat { get; set; }
        string? TimeStepIntervalLabelFormat { get; set; }
        TimeSliderLabelMode LabelMode { get; set; }
        Color ThumbStroke { get; set; }
        Color ThumbFill { get; set; }
        Color CurrentExtentFill { get; set; }
        Color FullExtentFill { get; set; }
        Color FullExtentStroke { get; set; }
        Color TimeStepIntervalTickFill { get; set; }
        Color PlaybackButtonsFill { get; set; }
        Color PlaybackButtonsStroke { get; set; }
        Color FullExtentLabelColor { get; set; }
        Color CurrentExtentLabelColor { get; set; }
        Color TimeStepIntervalLabelColor { get; set; }
        bool StepForward(int timeSteps);
        bool StepBack(int timeSteps);
    }
}