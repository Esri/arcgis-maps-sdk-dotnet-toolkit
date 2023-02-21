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
using System.Windows.Controls.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="AttachmentsPopupElement"/>.
    /// </summary>
    [TemplatePart(Name ="AttachmentList", Type= typeof(ListBox))]
    public class AttachmentsPopupElementView : Control
    {
        private ListBox? itemsList;
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentsPopupElementView"/> class.
        /// </summary>
        public AttachmentsPopupElementView()
        {
            DefaultStyleKey = typeof(AttachmentsPopupElementView);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
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

        private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var attachment = e.AddedItems[0] as Attachment;
                if(attachment != null)
                {
                    OnAttachmentClicked(attachment);
                }
                if (sender is Selector s)
                    s.SelectedValue = null;
            }
        }

        /// <summary>
        /// Occurs when an attachment is clicked.
        /// </summary>
        /// <remarks>Override this to prevent the default action.</remarks>
        /// <param name="attachment">Attachment clicked.</param>
        public virtual void OnAttachmentClicked(Attachment attachment)
        {
            if (GeoElement is ArcGISFeature feature && feature.FeatureTable is ServiceFeatureTable table && table.Source != null)
            {
                string uri = $"{table.Source.OriginalString}/{feature.Attributes[table.ObjectIdField]}/attachments/{attachment.Id}";
                _ = Launcher.LaunchUriAsync(new Uri(uri));
            }
        }

        private async void LoadAttachments()
        {
            if (itemsList is null) return;
            Visibility = Visibility.Collapsed;
            var feature = GeoElement as ArcGISFeature;
            if(feature is null || Element is null || feature.FeatureTable is ArcGISFeatureTable aft && !aft.HasAttachments)
            {
                itemsList.ItemsSource = null;
                return;
            }
            try
            {
                var attachments = await feature.GetAttachmentsAsync();
                itemsList.ItemsSource = attachments;
                Visibility = attachments.Count > 0 ? Visibility = Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
            }
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
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(AttachmentsPopupElement), typeof(AttachmentsPopupElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the GeoElement who's attachments will be showed with the <see cref="AttachmentsPopupElement"/>.
        /// </summary>
        public GeoElement? GeoElement
        {
            get => GetValue(GeoElementProperty) as GeoElement;
            set => SetValue(GeoElementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GeoElement"/> dependency property.
        /// </summary>       
        public static readonly DependencyProperty GeoElementProperty =
            DependencyProperty.Register(nameof(GeoElement), typeof(GeoElement), typeof(AttachmentsPopupElementView), new PropertyMetadata(null, (s, e) => ((AttachmentsPopupElementView)s).LoadAttachments()));

    }
}