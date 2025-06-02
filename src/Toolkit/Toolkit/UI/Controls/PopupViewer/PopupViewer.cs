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

using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.RealTime;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;


#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using ScrollViewer = Microsoft.Maui.Controls.ScrollView;
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
#elif WPF
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            InvalidatePopup();
        }


        private bool _isDirty = false;
        private object _isDirtyLock = new object();

#if MAUI
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.Popups.Popup.EvaluatedElements), "Esri.ArcGISRuntime.Mapping.Popups.Popup", "Esri.ArcGISRuntime")]
#endif
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
#elif WPF
            _ = Dispatcher.InvokeAsync(async () =>
#elif WINUI
            DispatcherQueue.TryEnqueue(async () =>
#elif WINDOWS_UWP
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
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
#if MAUI
                        var ctrl = GetTemplateChild(ItemsViewName) as IBindableLayout;
                        if (ctrl != null && ctrl is BindableObject bo)
                        {
                            bo.SetBinding(BindableLayout.ItemsSourceProperty, static (PopupViewer viewer) => viewer.Popup?.EvaluatedElements, source: RelativeBindingSource.TemplatedParent);
                        }
#else
                        var ctrl = GetTemplateChild(ItemsViewName) as ItemsControl;
                        var binding = ctrl?.GetBindingExpression(ItemsControl.ItemsSourceProperty);
#if WPF
                        binding?.UpdateTarget();
#elif WINDOWS_XAML
                        ctrl?.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Popup.EvaluatedElements"), Source = this });
#endif
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
        public static readonly DependencyProperty PopupProperty =
            PropertyHelper.CreateProperty<Popup, PopupViewer>(nameof(Popup), null, (s, oldValue, newValue) => s.OnPopupPropertyChanged(oldValue, newValue));

        private void OnPopupPropertyChanged(Popup? oldPopup, Popup? newPopup)
        {
            if (oldPopup?.GeoElement is not null)
            {
                _dynamicEntityChangedListener?.Detach();
                _dynamicEntityChangedListener = null;
                _geoElementPropertyChangedListener?.Detach();
                _geoElementPropertyChangedListener = null;
            }
            if (newPopup?.GeoElement is not null)
            {
                if(newPopup.GeoElement is DynamicEntity de)
                {
                    _dynamicEntityChangedListener = new WeakEventListener<PopupViewer, DynamicEntity, object?, DynamicEntityChangedEventArgs>(this, de)
                    {
                        OnEventAction = static (instance, source, eventArgs) => instance.InvalidatePopup(),
                        OnDetachAction = static (instance, source, weakEventListener) => source.DynamicEntityChanged -= weakEventListener.OnEvent,
                    };
                    de.DynamicEntityChanged += _dynamicEntityChangedListener.OnEvent;
                }
                else if (newPopup.GeoElement is INotifyPropertyChanged inpc)
                {
                    _geoElementPropertyChangedListener = new WeakEventListener<PopupViewer, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpc)
                    {
                        OnEventAction = static (instance, source, eventArgs) => instance.InvalidatePopup(),
                        OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                    };
                    inpc.PropertyChanged += _geoElementPropertyChangedListener.OnEvent;

                }
            }
            InvalidatePopup();
#if MAUI
            (GetTemplateChild(PopupContentScrollViewerName) as ScrollViewer)?.ScrollToAsync(0,0,false);
#elif WPF
            (GetTemplateChild(PopupContentScrollViewerName) as ScrollViewer)?.ScrollToHome();
#elif WINDOWS_XAML
            (GetTemplateChild(PopupContentScrollViewerName) as ScrollViewer)?.ChangeView(null, 0, null, disableAnimation: true);
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
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            PropertyHelper.CreateProperty<ScrollBarVisibility, PopupViewer>(nameof(VerticalScrollBarVisibility),
#if MAUI
                ScrollBarVisibility.Default
#else
                ScrollBarVisibility.Auto
#endif
                );

        /// <summary>
        /// Raised when a popup attachment is clicked
        /// </summary>
        /// <remarks>
        /// <para>By default, when an attachment is clicked, the default application for the file type (if any) is launched. To override this,
        /// listen to this event, set the <see cref="PopupAttachmentClickedEventArgs.Handled"/> property to <c>true</c> and perform
        /// your own logic. </para>
        /// <example>
        /// Example: Use the .NET MAUI share API for the attachment:
        /// <code language="csharp">
        /// private async void PopupAttachmentClicked(object sender, PopupAttachmentClickedEventArgs e)
        /// {
        ///     e.Handled = true; // Prevent default launch action
        ///     await Share.Default.RequestAsync(new ShareFileRequest(new ReadOnlyFile(e.Attachment.Filename!, e.Attachment.ContentType)));
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        public event EventHandler<PopupAttachmentClickedEventArgs>? PopupAttachmentClicked;

        internal bool OnPopupAttachmentClicked(PopupAttachment attachment)
        {
            var handler = PopupAttachmentClicked;
            if (handler is not null)
            {
                var args = new PopupAttachmentClickedEventArgs(attachment);
                handler.Invoke(this, args);
                return args.Handled;
            }
            return false;
        }

        /// <summary>
        /// Raised when a link is clicked
        /// </summary>
        /// <remarks>
        /// <para>By default, when an link is clicked, the default application (Browser) for the file type (if any) is launched. To override this,
        /// listen to this event, set the <see cref="HyperlinkClickedEventArgs.Handled"/> property to <c>true</c> and perform
        /// your own logic. </para>
        /// </remarks>
        public event EventHandler<HyperlinkClickedEventArgs>? HyperlinkClicked;

        internal void OnHyperlinkClicked(Uri uri)
        {
            var handler = HyperlinkClicked;
            if (handler is not null)
            {
                var args = new HyperlinkClickedEventArgs(uri);
                handler.Invoke(this, args);
                if (args.Handled)
                    return;
            }
            Launcher.LaunchUriAsync(uri);
        }
    }

    /// <summary>
    /// Event argument for the <see cref="PopupViewer.PopupAttachmentClicked"/> event.
    /// </summary>
    public class PopupAttachmentClickedEventArgs : EventArgs
    {
        internal PopupAttachmentClickedEventArgs(PopupAttachment attachment)
        {
            Attachment = attachment;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event handler has handled the event and the default action should be prevented.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the attachment that was clicked.
        /// </summary>
        public PopupAttachment Attachment { get; }
    }

    /// <summary>
    /// Event argument for the <see cref="PopupViewer.HyperlinkClicked"/> event.
    /// </summary>
    public sealed class HyperlinkClickedEventArgs : EventArgs
    {
        internal HyperlinkClickedEventArgs(Uri uri)
        {
            Uri = uri;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event handler has handled the event and the default action should be prevented.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the URI that was clicked.
        /// </summary>
        public Uri Uri { get; }
    }
}