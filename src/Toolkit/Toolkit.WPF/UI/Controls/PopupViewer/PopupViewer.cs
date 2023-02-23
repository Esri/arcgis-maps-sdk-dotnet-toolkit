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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The PopupViewer control is used to display details and media, edit attributes, geometry and related records,
    /// manage attachments of an <see cref="Data.ArcGISFeature"/> or a <see cref="ArcGISRuntime.UI.Graphic"/>
    /// as defined in its <see cref="Mapping.Popups.Popup"/>.
    /// </summary>
    [TemplatePart(Name = "PopupContentScrollViewer", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "ItemsView", Type = typeof(ItemsControl))]
    public partial class PopupViewer : Control
    {
        private WeakEventListener<PopupViewer, Mapping.DynamicEntity, object?, DynamicEntityChangedEventArgs>? _dynamicEntityChangedListener;
        private WeakEventListener<PopupViewer, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _geoElementPropertyChangedListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupViewer"/> class.
        /// </summary>
        public PopupViewer()
            : base()
        {
            DefaultStyleKey = typeof(PopupViewer);
        }

        /// <inheritdoc/>
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            InvalidatePopup();
        }

        private bool _isDirty = false;
        private object _isDirtyLock = new object();

        private void InvalidatePopup()
        {
            lock (_isDirtyLock)
            {
                if (_isDirty)
                {
                    Debug.WriteLine("Already dirty - skipping update");
                    return;
                }
                Debug.WriteLine("Initiating cleanup");
                _isDirty = true;
            }
            _ = Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    lock (_isDirtyLock)
                    {
                        _isDirty = false;
                        Debug.WriteLine("Cleanup complete");
                    }
                    if (Popup != null)
                    {
                        var expressions = await Popup.EvaluateExpressionsAsync();
                        var ctrl = GetTemplateChild("ItemsView") as ItemsControl;
                        var binding = ctrl?.GetBindingExpression(ItemsControl.ItemsSourceProperty);
                        binding?.UpdateTarget();
                    }
                }
                catch
                {
                }
            });
        }

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

        /// <summary>
        /// Gets or sets the associated PopupManager which contains popup and sketch editor.
        /// </summary>
        public Popup? Popup
        {
            get { return GetValue(PopupProperty) as Popup; }
            set { SetValue(PopupProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PopupManager"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PopupProperty =
            DependencyProperty.Register(nameof(Popup), typeof(Popup), typeof(PopupViewer),
                new PropertyMetadata(null, OnPopupPropertyChanged));

        private static void OnPopupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var popupViewer = (PopupViewer)d;
            var oldPopup = e.OldValue as Popup;
            if (oldPopup?.GeoElement is not null)
            {
                popupViewer._dynamicEntityChangedListener?.Detach();
                popupViewer._dynamicEntityChangedListener = null;
                popupViewer._geoElementPropertyChangedListener?.Detach();
                popupViewer._geoElementPropertyChangedListener = null;
            }
            var newPopup = e.NewValue as Popup;
            if(newPopup?.GeoElement is not null)
            {
                if(newPopup.GeoElement is Mapping.DynamicEntity de)
                {
                    popupViewer._dynamicEntityChangedListener = new WeakEventListener<PopupViewer, Mapping.DynamicEntity, object?, DynamicEntityChangedEventArgs>(popupViewer, de)
                    {
                        OnEventAction = static (instance, source, eventArgs) => instance.InvalidatePopup(),
                        OnDetachAction = static (instance, source, weakEventListener) => source.DynamicEntityChanged -= weakEventListener.OnEvent,
                    };
                    de.DynamicEntityChanged += popupViewer._dynamicEntityChangedListener.OnEvent;
                }
                else if (newPopup.GeoElement is INotifyPropertyChanged inpc)
                {
                    popupViewer._geoElementPropertyChangedListener = new WeakEventListener<PopupViewer, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(popupViewer, inpc)
                    {
                        OnEventAction = static (instance, source, eventArgs) => instance.InvalidatePopup(),
                        OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                    };
                    inpc.PropertyChanged += popupViewer._geoElementPropertyChangedListener.OnEvent;

                }
            }
            popupViewer.InvalidatePopup();
            (popupViewer.GetTemplateChild("PopupContentScrollViewer") as ScrollViewer)?.ScrollToHome();
        }

        /// <summary>
        /// Gets or sets the vertical scrollbar visibility of the scrollviewer below the title.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VerticalScrollBarVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(PopupViewer), new PropertyMetadata(ScrollBarVisibility.Auto));
    }
}