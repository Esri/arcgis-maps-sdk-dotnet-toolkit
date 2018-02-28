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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Base class for <see cref="ReadOnlyDataItem"/>, <see cref="InputDataItem"/> and <see cref="SelectorDataItem"/>.
    /// </summary>
    internal abstract class DataItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Callback for notifying <see cref="FeatureDataField"/> that <see cref="Value"/> property has changed.
        /// </summary>
        private readonly Action<object> _callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataItem" /> class.
        /// </summary>
        /// <param name="callback">callback raised when <see cref="Value"/> property changes.</param>
        internal DataItem(Action<object> callback)
        {
            _callback = callback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataItem" /> class.
        /// </summary>
        /// <param name="callback">callback raised when <see cref="Value"/> property changes.</param>
        /// <param name="value">default value.</param>
        internal DataItem(Action<object> callback, object value)
            : this(callback)
        {
            _value = value;
        }

        /// <summary>
        /// Raises <see cref="_callback"/> with the current <see cref="Value"/> property.
        /// </summary>
        protected virtual void OnValueChanged() => _callback?.Invoke(GetBoundValue());

        internal virtual object GetBoundValue() => Value;

        private object _value;

        /// <summary>
        /// Gets or sets the value used to bind ContentTemplate.
        /// </summary>
        public virtual object Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;
                OnPropertyChanged();
                OnValueChanged();
            }
        }

        private string _errorMessage;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public virtual string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }

            set
            {
                if (_errorMessage == value)
                {
                    return;
                }

                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
#endif