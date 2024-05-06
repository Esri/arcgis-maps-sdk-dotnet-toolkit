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


#if MAUI
global using DependencyProperty = Microsoft.Maui.Controls.BindableProperty;
using DependencyObject = Microsoft.Maui.Controls.BindableObject;
#endif
namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class PropertyHelper
    {
        /// <summary>
        /// Abstracts away the different syntax for declaring a dependency property in MAUI, UWP, WinUI and WPF
        /// </summary>
        /// <typeparam name="ValueType">The property type</typeparam>
        /// <typeparam name="OwnerType">The type this dependency property is declared on (ie the owner)</typeparam>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="defaultValue">The default value</param>
        /// <param name="propertyChanged">Method to call when value changes</param>
        /// <returns></returns>
        public static DependencyProperty CreateProperty<ValueType, OwnerType>(string propertyName,
            ValueType? defaultValue = default,
            Action<OwnerType, ValueType?, ValueType?>? propertyChanged = null) where OwnerType : DependencyObject
#if MAUI
             => DependencyProperty.Create(propertyName, typeof(ValueType), typeof(OwnerType), defaultValue, propertyChanged: propertyChanged is null ? null :
                (s, oldValue, newValue) => propertyChanged?.Invoke((OwnerType)s, oldValue is null ? default : (ValueType)oldValue, newValue is null ? default : (ValueType)newValue));
#else
            => DependencyProperty.Register(propertyName, typeof(ValueType), typeof(OwnerType),
                new PropertyMetadata(defaultValue,
                    propertyChanged is null ? null :
                    (s, e) => propertyChanged?.Invoke((OwnerType)s, e.OldValue is null ? default : (ValueType)e.OldValue, e.NewValue is null ? default : (ValueType)e.NewValue)));
#endif
    }
}

    