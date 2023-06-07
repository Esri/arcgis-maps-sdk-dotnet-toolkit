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

#if WPF || MAUI
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.RealTime;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using DependencyObject = Microsoft.Maui.Controls.BindableObject;
using ScrollViewer = Microsoft.Maui.Controls.ScrollView;
using ItemsControl = Microsoft.Maui.Controls.CollectionView;
#else
using Esri.ArcGISRuntime.Toolkit.Primitives;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// The PopupViewer control is used to display details and media, edit attributes, geometry and related records,
    /// manage attachments of an <see cref="Data.ArcGISFeature"/> or a <see cref="ArcGISRuntime.UI.Graphic"/>
    /// as defined in its <see cref="Mapping.Popups.Popup"/>.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="PopupViewer"/> consists of a number of sub-controls all in the <see cref="Primitives"/> namespace.
    /// <list type="bullet">
    ///  <listheader>
    ///     <term>Control</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item><term><see cref="AttachmentsPopupElementView"/></term><description>Displays and downloads the attachments defind by the <see cref="AttachmentsPopupElement"/>.</description></item>
    ///   <item><term><see cref="FieldsPopupElementView"/></term><description>Displays the feature fields defined by the <see cref="FieldsPopupElement"/>.</description></item>
    ///   <item><term><see cref="MediaPopupElementView"/></term><description>Displays the images and charts defined by the <see cref="MediaPopupElement"/>.</description></item>
    ///   <item><term><see cref="TextPopupElementView"/></term><description>Displays the text content defined by the <see cref="TextPopupElement"/>.</description></item>
    /// </list>
    /// </para>
    /// <para>In addition to overwrite the control templates of this control and its child controls, the following styles are available for overriding text styling:
    /// <list type="bullet">
    ///  <listheader>
    ///     <term>Resource Key</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item><term>PopupViewerHeaderStyle</term><description>Label style for the main popup header.</description></item>
    ///   <item><term>PopupViewerTitleStyle</term><description>Label style for the title of each popup element.</description></item>
    ///   <item><term>PopupViewerCaptionStyle</term><description>Label style for the caption of each popup element.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class PopupViewer
    {
        private WeakEventListener<PopupViewer, DynamicEntity, object?, DynamicEntityChangedEventArgs>? _dynamicEntityChangedListener;
        private WeakEventListener<PopupViewer, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _geoElementPropertyChangedListener;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PopupViewer"/> class.
        /// </summary>
        public PopupViewer()
            : base()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(PopupViewer);
#endif
        }
        /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
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
                    return;
                }
                _isDirty = true;
            }
#if MAUI
            Dispatcher.Dispatch(async () =>
#else
            _ = Dispatcher.InvokeAsync(async () =>
#endif
            {
                try
                {
                    lock (_isDirtyLock)
                    {
                        _isDirty = false;
                    }
                    if (Popup != null)
                    {
                        var expressions = await Popup.EvaluateExpressionsAsync();
                        var ctrl = GetTemplateChild(ItemsViewName) as ItemsControl;

#if MAUI
                        if (ctrl != null)
                        {
                            ctrl.ItemsSource = Popup.EvaluatedElements; // TODO: Should update binding instead
                        }
#else
                        var binding = ctrl?.GetBindingExpression(ItemsControl.ItemsSourceProperty);
                        binding?.UpdateTarget();
#endif
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
        public Popup? Popup
        {
            get { return GetValue(PopupProperty) as Popup; }
            set { SetValue(PopupProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PopupManager"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty PopupProperty =
            BindableProperty.Create(nameof(Popup), typeof(Popup), typeof(PopupViewer), null, propertyChanged: OnPopupPropertyChanged);
#else
        public static readonly DependencyProperty PopupProperty =
            DependencyProperty.Register(nameof(Popup), typeof(Popup), typeof(PopupViewer),
                new PropertyMetadata(null, (s,e) => PopupViewer.OnPopupPropertyChanged(s, e.OldValue, e.NewValue)));
#endif

        private static void OnPopupPropertyChanged(DependencyObject d, object oldValue, object newValue)
        {
            var popupViewer = (PopupViewer)d;
            var oldPopup = oldValue as Popup;
            if (oldPopup?.GeoElement is not null)
            {
                popupViewer._dynamicEntityChangedListener?.Detach();
                popupViewer._dynamicEntityChangedListener = null;
                popupViewer._geoElementPropertyChangedListener?.Detach();
                popupViewer._geoElementPropertyChangedListener = null;
            }
            var newPopup = newValue as Popup;
            if (newPopup?.GeoElement is not null)
            {
                if(newPopup.GeoElement is DynamicEntity de)
                {
                    popupViewer._dynamicEntityChangedListener = new WeakEventListener<PopupViewer, DynamicEntity, object?, DynamicEntityChangedEventArgs>(popupViewer, de)
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
#if MAUI
            (popupViewer.GetTemplateChild(PopupContentScrollViewerName) as ScrollViewer)?.ScrollToAsync(0,0,false);
#else
            (popupViewer.GetTemplateChild(PopupContentScrollViewerName) as ScrollViewer)?.ScrollToHome();
#endif
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
#if MAUI
        public static readonly BindableProperty VerticalScrollBarVisibilityProperty =
            BindableProperty.Create(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(PopupViewer), ScrollBarVisibility.Default);
#else
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(PopupViewer), new PropertyMetadata(ScrollBarVisibility.Auto));
#endif
    }
}
#endif