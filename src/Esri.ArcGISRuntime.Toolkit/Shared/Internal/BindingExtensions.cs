﻿// /*******************************************************************************
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

#if !NETFX_CORE && !__IOS__ && !__ANDROID__

using System.Windows;
using System.Windows.Data;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Provides utility extension methods related to data-bindings
    /// </summary>
    static internal class BindingExtensions
    {
        /// <summary>
        /// Creates a copy of the specified binding
        /// </summary>
        /// <param name="source">The binding to copy</param>
        /// <returns>The copy of the binding</returns>
        public static Binding Clone(this Binding source)
        {
            var copy = new Binding
            {
                AsyncState = source.AsyncState,
                BindingGroupName = source.BindingGroupName,
                BindsDirectlyToSource = source.BindsDirectlyToSource,
                Converter = source.Converter,
                ConverterCulture = source.ConverterCulture,
                ConverterParameter = source.ConverterParameter,
                FallbackValue = source.FallbackValue,
                IsAsync = source.IsAsync,
                Mode = source.Mode,
                NotifyOnSourceUpdated = source.NotifyOnSourceUpdated,
                NotifyOnTargetUpdated = source.NotifyOnTargetUpdated,
                NotifyOnValidationError = source.NotifyOnValidationError,
                Path = source.Path,
                StringFormat = source.StringFormat,
                TargetNullValue = source.TargetNullValue,
                UpdateSourceExceptionFilter = source.UpdateSourceExceptionFilter,
                UpdateSourceTrigger = source.UpdateSourceTrigger,
                ValidatesOnDataErrors = source.ValidatesOnDataErrors,
                ValidatesOnExceptions = source.ValidatesOnExceptions,
                XPath = source.XPath,
            };
            if (source.Source != null)
            {
                copy.Source = source.Source;
            }
            else if (source.ElementName != null)
            {
                copy.ElementName = source.ElementName;
            }
            else if (source.RelativeSource != null)
            {
                copy.RelativeSource = source.RelativeSource;
            }

            return copy;
        }

        /// <summary>
        /// Applies the specified format string to the binding of the specified property on the specified element.  Also initializes a fallback string format from the
        /// original string format if one is not already referenced in the corresponding parameter.
        /// </summary>
        /// <param name="bindingTarget">The element for which to update the property</param>
        /// <param name="targetProperty">The dependency property to update</param>
        /// <param name="stringFormat">The new format string</param>
        /// <param name="fallbackFormat">The fallback format string.  Used if the new format string is null or empty.</param>
        public static void UpdateStringFormat(this FrameworkElement bindingTarget, DependencyProperty targetProperty, string stringFormat, ref string fallbackFormat)
        {
            if (bindingTarget == null)
                return;

            // Get the binding for the target property
            var binding = bindingTarget.GetBindingExpression(targetProperty)?.ParentBinding;
            if (binding == null)
                return; // Or should we create a new binding in this case?

            // If the fallback hasn't been populated already, store the current string format as specified by the binding
            if (fallbackFormat == null)
                fallbackFormat = binding.StringFormat;

            // Create a new binding to apply the format string.  Necessary because bindings that are already in use cannot be updated,
            // but we want to preserve how users may have setup the binding otherwise
            var newBinding = binding.Clone();

            // If the format string is null or empty, use the fall back format.  Otherwise, apply the new format.
            newBinding.StringFormat = !string.IsNullOrEmpty(stringFormat) ? stringFormat : fallbackFormat;
            bindingTarget.SetBinding(targetProperty, newBinding);
        }
    }
}

#endif