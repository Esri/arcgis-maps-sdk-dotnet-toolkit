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

using System;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Event args used for <see cref="FeatureDataField.ValueChanging"/> event.
    /// </summary>
    public sealed class ValueChangingEventArgs : ValueEventArgs
    {
        internal ValueChangingEventArgs(object oldValue, object newValue)
            : base(oldValue, newValue)
        {
        }

        /// <summary>
        /// Gets or sets the user-defined validation exception.
        /// </summary>
        public Exception ValidationException { get; set; }
    }
}
