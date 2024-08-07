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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.RealTime;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = PopupContentScrollViewerName, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = ItemsViewName, Type = typeof(ItemsControl))]
    public partial class PopupViewer : Control
    {
        private const string ItemsViewName = "ItemsView";
        private const string PopupContentScrollViewerName = "PopupContentScrollViewer";

        /// <summary>
        /// Gets or sets the associated PopupManager which contains popup and sketch editor.
        /// </summary>
        [Obsolete("Use Popup property.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PopupManager? PopupManager
        {
            get { return GetValue(PopupManagerProperty) as PopupManager; }
            set { SetValue(PopupManagerProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PopupManager"/> dependency property.
        /// </summary>
        [Obsolete("Use PopupProperty field.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty PopupManagerProperty =
            DependencyProperty.Register(nameof(PopupManager), typeof(PopupManager), typeof(PopupViewer),
                new PropertyMetadata(null, OnPopupManagerPropertyChanged));

        private static void OnPopupManagerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ((PopupViewer)d).Popup = ((PopupViewer)d).PopupManager?.Popup;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        internal static PopupViewer? GetPopupViewerParent(DependencyObject? child) => GetParent<PopupViewer>(child);

        private static T? GetParent<T>(DependencyObject? child) where T : DependencyObject
        {
#if WPF
            if (child is FrameworkContentElement elm)
            {
                child = GetVisualParent(elm);
            }
#endif
            if (child is null)
                return default;
            var parent = VisualTreeHelper.GetParent(child);
            while (parent is not null and not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

#if WPF
        private static Visual? GetVisualParent(FrameworkContentElement child)
        {
            var parent = child.Parent;
            while (parent is FrameworkContentElement elm)
            {
                parent = elm.Parent;
            }
            return parent as Visual;
        }
#endif
    }
}
#endif