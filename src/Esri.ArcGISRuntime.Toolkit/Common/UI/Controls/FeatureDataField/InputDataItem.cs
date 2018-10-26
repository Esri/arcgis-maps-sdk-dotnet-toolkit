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
    /// InputDataItem is a bindable object for <see cref="FeatureDataField.InputTemplate"/>.
    /// </summary>
    internal sealed class InputDataItem : DataItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDataItem" /> class.
        /// </summary>
        /// <param name="callback">callback raised when <see cref="DataItem.Value"/> property changes.</param>
        /// <param name="value">default value.</param>
        /// <param name="field">contains schema for field value.</param>
        internal InputDataItem(Action<object> callback, object value, Field field)
            : base(callback, value)
        {
            Field = field;
        }

        private Field _field;

        public Field Field
        {
            get
            {
                return _field;
            }

            set
            {
                if (_field == value)
                {
                    return;
                }

                _field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
            }
        }
    }
}
#endif