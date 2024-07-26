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
using System.Diagnostics;
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a set of popup elements.
    /// </summary>
    public class PopupElementItemsControl : ItemsControl
    {
        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is ContentPresenter presenter)
            {
                if (item is TextPopupElement)
                {
                    presenter.ContentTemplate = TextPopupElementTemplate;
                }
                else if (item is MediaPopupElement)
                {
                    presenter.ContentTemplate = MediaPopupElementTemplate;
                }
                else if (item is FieldsPopupElement)
                {
                    presenter.ContentTemplate = FieldsPopupElementTemplate;
                }
                else if (item is AttachmentsPopupElement)
                {
                    presenter.ContentTemplate = AttachmentsPopupElementTemplate;
                }
                /* Excluded for now - Pending UI Experience design
                else if(item is RelationshipPopupElement)
                {
                    presenter.ContentTemplate = RelationshipPopupElementTemplate;
                } */
                else if (item is ExpressionPopupElement)
                {
                    // This shouldn't happen since the evaluated elements are
                    // always returned as TextPopupElement.
                    Debug.Assert(false);
                }
            }
        }

        /// <summary>
        /// Template used for rendering a <see cref="TextPopupElement"/>.
        /// </summary>
        /// <seealso cref="TextPopupElementView"/>
        public DataTemplate TextPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(TextPopupElementTemplateProperty); }
            set { SetValue(TextPopupElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TextPopupElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(TextPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="MediaPopupElement"/>.
        /// </summary>
        /// <seealso cref="MediaPopupElementView"/>
        public DataTemplate MediaPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(MediaPopupElementTemplateProperty); }
            set { SetValue(MediaPopupElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MediaPopupElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MediaPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(MediaPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="FieldsPopupElement"/>.
        /// </summary>
        /// <seealso cref="FieldsPopupElementView"/>
        public DataTemplate FieldsPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(FieldsPopupElementTemplateProperty); }
            set { SetValue(FieldsPopupElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FieldsPopupElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FieldsPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(FieldsPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="AttachmentsPopupElement"/>.
        /// </summary>
        /// <seealso cref="AttachmentsPopupElementView"/>
        public DataTemplate AttachmentsPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(AttachmentsPopupElementTemplateProperty); }
            set { SetValue(AttachmentsPopupElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AttachmentsPopupElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AttachmentsPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(AttachmentsPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));

        /* Excluded for now - Pending UI Experience design
        /// <summary>
        /// Template used for rendering a <see cref="RelationshipPopupElement"/>.
        /// </summary>
        /// <seealso cref="RelationshipPopupElementView"/>
        public DataTemplate RelationshipPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(RelationshipPopupElementTemplateProperty); }
            set { SetValue(RelationshipPopupElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RelationshipPopupElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RelationshipPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(RelationshipPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));*/
    }
}
#endif