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
using Microsoft.Win32;
#if WPF
using System.Windows.Controls.Primitives;
#else
using ListBox = Microsoft.Maui.Controls.CollectionView;
using Selector = Microsoft.Maui.Controls.SelectableItemsView;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class AttachmentsPopupElementView
    {
        private ListBox? itemsList;
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentsPopupElementView"/> class.
        /// </summary>
        public AttachmentsPopupElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(AttachmentsPopupElementView);
#endif
        }

        /// <inheritdoc />
#if MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            itemsList = GetTemplateChild("AttachmentList") as ListBox;
            if (itemsList != null)
            {
                itemsList.SelectionMode = SelectionMode.Single;
                itemsList.SelectionChanged += ItemsList_SelectionChanged;
                LoadAttachments();
            }
        }

        private void ItemsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
#if MAUI
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                var attachment = e.CurrentSelection[0] as PopupAttachment;
#else
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var attachment = e.AddedItems[0] as PopupAttachment;
#endif
                if (attachment?.Attachment != null)
                {
                    OnAttachmentClicked(attachment);
                }
                if (sender is Selector s)
                    s.SelectedItem = null;
            }
        }

        private async void LoadAttachments()
        {
            if (itemsList is null) return;
#if MAUI
            IsVisible = false;
#else
            Visibility = Visibility.Collapsed;
#endif
            itemsList.ItemsSource = null;
            if (Element is not null)
            {
                try
                {
                    await Element.GetAttachmentsAsync();
                }
                catch
                {

                }
                itemsList.ItemsSource = Element?.Attachments;
            }
            bool isVisible = (Element?.Attachments?.Count ?? 0) > 0;
#if MAUI
            IsVisible = isVisible;
#else
            Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
#endif

        }

        /// <summary>
        /// Gets or sets the AttachmentsPopupElement.
        /// </summary>
        public AttachmentsPopupElement? Element
        {
            get { return GetValue(ElementProperty) as AttachmentsPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(AttachmentsPopupElement), typeof(AttachmentsPopupElementView), null, propertyChanged: (s, o, n) => ((AttachmentsPopupElementView)s).LoadAttachments());
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(AttachmentsPopupElement), typeof(AttachmentsPopupElementView), new PropertyMetadata(null, (s, e) => ((AttachmentsPopupElementView)s).LoadAttachments()));
#endif
    }
}
#endif