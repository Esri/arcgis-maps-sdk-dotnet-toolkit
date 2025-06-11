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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A visual feature editor form controlled by a <see cref="FeatureForm"/> definition.
    /// </summary>
    /// <seealso cref="Esri.ArcGISRuntime.Data.ArcGISFeatureTable.FeatureFormDefinition"/>
    /// <seealso cref="Esri.ArcGISRuntime.Mapping.FeatureLayer.FeatureFormDefinition"/>
    [TemplatePart(Name = FeatureFormContentScrollViewerName, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = ItemsViewName, Type = typeof(ItemsControl))]
    public partial class FeatureFormView : Control
    {
        private const string ItemsViewName = "ItemsView";
        private const string FeatureFormContentScrollViewerName = "FeatureFormContentScrollViewer";

#if WPF
        private static readonly DependencyPropertyKey IsValidPropertyKey
          = DependencyProperty.RegisterReadOnly(nameof(IsValid), typeof(bool), typeof(FeatureFormView), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsValid"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty IsValidProperty = IsValidPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether this form has any validation errors.
        /// </summary>
        /// <seealso cref="FeatureForm.ValidationErrors"/>
        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            private set { SetValue(IsValidPropertyKey, value); }
        }
#endif

        internal static UI.Controls.FeatureFormView? GetFeatureFormViewParent(DependencyObject? child) => GetParent<FeatureFormView>(child);

        internal static T? GetParent<T>(DependencyObject? child) where T : FrameworkElement
        {
            if (child is null) return default(T);
            var parent = VisualTreeHelper.GetParent(child);
            while (parent is not null && parent is not T view)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        internal static IEnumerable<T> GetDescendentsOfType<T>(FrameworkElement root)
        {
            if (root is null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is FrameworkElement frameworkElement)
                {
                    if (frameworkElement is T targetElement)
                        yield return targetElement;

                    foreach (var descendant in GetDescendentsOfType<T>(frameworkElement))
                        yield return descendant;
                }
            }
        }

        private FeatureForm? GetCurrentFeatureForm()
        {
#if WINDOWS_XAML
            return (FeatureForm?)GetValue(CurrentFeatureFormProperty);
#elif WPF
            return (FeatureForm?)GetValue(CurrentFeatureFormPropertyKey.DependencyProperty);
#endif
        }

        private void SetCurrentFeatureForm(FeatureForm? value)
        {
            var oldValue = CurrentFeatureForm;
            if (oldValue != value)
            {
#if WINDOWS_XAML
                SetValue(CurrentFeatureFormProperty, value);
#elif WPF
                SetValue(CurrentFeatureFormPropertyKey, value);
#endif
                OnCurrentFeatureFormPropertyChanged(oldValue, value);
            }
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty CurrentFeatureFormProperty =
            DependencyProperty.Register(nameof(CurrentFeatureForm), typeof(FeatureForm), typeof(FeatureFormView), new PropertyMetadata(null));
#else
        private static readonly DependencyPropertyKey CurrentFeatureFormPropertyKey =
                DependencyProperty.RegisterReadOnly(
                  name: nameof(CurrentFeatureForm),
                  propertyType: typeof(FeatureForm),
                  ownerType: typeof(FeatureFormView),
                  typeMetadata: new FrameworkPropertyMetadata());
#endif
    }
}
#endif