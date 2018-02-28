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

using System;
using Esri.ArcGISRuntime.Data;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// ReadOnlyDataItem is a bindable object for <see cref="FeatureDataField.ReadOnlyTemplate"/>.
    /// </summary>
    internal sealed class ReadOnlyDataItem : DataItem
    {
        private readonly Field _field;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyDataItem" /> class.
        /// </summary>
        /// <param name="value">default value.</param>
        /// <param name="field">contains schema for field value.</param>
        internal ReadOnlyDataItem(object value, Field field)
            : base(null, value)
        {
            _value = value;
            _field = field;
        }

        private object _value;

        /// <summary>
        /// Gets or sets the current attribute value of field.
        /// </summary>
        public override object Value
        {
            get { return _field?.GetDisplayValue(_value) ?? _value; }
            set { _value = value; }
        }
    }
}
#endif