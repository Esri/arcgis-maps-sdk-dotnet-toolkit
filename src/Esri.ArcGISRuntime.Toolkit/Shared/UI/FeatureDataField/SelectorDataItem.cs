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
using System.Collections.Generic;
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// SelectorDataItem is a bindable object for <see cref="FeatureDataField.SelectorTemplate"/>.
    /// </summary>
#if NETFX_CORE
    [Bindable]
#endif
    internal sealed class SelectorDataItem : DataItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorDataItem" /> class.
        /// </summary>
        /// <param name="callback">callback raised when <see cref="DataItem.Value"/> property changes.</param>
        /// <param name="value">default value selected.</param>
        /// <param name="items">coded-value domain options.</param>
        public SelectorDataItem(Action<object> callback, object value, IEnumerable<KeyValuePair<object, string>> items)
            : base(callback)
        {
            Items = items?.ToList();
            Value = GetSelectedItem(value);
        }

        /// <summary>
        /// Returns the matching key-value from <see cref="Items"/>.
        /// </summary>
        /// <param name="value">key value.</param>
        /// <returns>matching key-value.</returns>
        public KeyValuePair<object, string> GetSelectedItem(object value)
        {
            return Items?.FirstOrDefault(kvp => kvp.Key != null && kvp.Key.Equals(value)) ??
                default(KeyValuePair<object, string>);
        }

        private IList<KeyValuePair<object, string>> _items;

        /// <summary>
        /// Gets or sets the Items used as source for <see cref="FeatureDataField.SelectorTemplate"/>
        /// </summary>
        public IList<KeyValuePair<object, string>> Items
        {
            get
            {
                return _items;
            }

            set
            {
                if (_items == value)
                {
                    return;
                }

                _items = value;
                OnValueChanged();
            }
        }
    }
}