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
using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting selector for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a set of popup elements.
    /// </summary>
    public class PopupElementTemplateSelector : DataTemplateSelector
    {
        private static DataTemplate DefaultTextPopupElementTemplate;
        private static DataTemplate DefaultMediaPopupElementTemplate;
        private static DataTemplate DefaultFieldsPopupElementTemplate;
        private static DataTemplate DefaultAttachmentsPopupElementTemplate;
        private static DataTemplate DefaultUtilityAssociationsPopupElementTemplate;
        private static DataTemplate UnsupportedPopupElementTemplate;

        static PopupElementTemplateSelector()
        {
            DefaultTextPopupElementTemplate = new DataTemplate(() =>
            {
                var view = new TextPopupElementView() { Margin = new Thickness(0, 10) };
                view.SetBinding(TextPopupElementView.ElementProperty, static (PopupElement element) => element);
                return view;
            });
            DefaultMediaPopupElementTemplate = new DataTemplate(() =>
            {
                var view = new MediaPopupElementView() { Margin = new Thickness(0, 10) };
                view.SetBinding(MediaPopupElementView.ElementProperty, static (PopupElement element) => element);
                return view;
            });
            DefaultFieldsPopupElementTemplate = new DataTemplate(() =>
            {
                var view = new FieldsPopupElementView() { Margin = new Thickness(0, 10) };
                view.SetBinding(FieldsPopupElementView.ElementProperty, static (PopupElement element) => element);
                return view;
            });
            
            DefaultAttachmentsPopupElementTemplate = new DataTemplate(() =>
            {
                var view = new AttachmentsPopupElementView() { Margin = new Thickness(0, 10) };
                view.SetBinding(AttachmentsPopupElementView.ElementProperty, static (PopupElement element) => element);
                return view;
            });

            DefaultUtilityAssociationsPopupElementTemplate = new DataTemplate(() =>
            {
                var view = new UtilityAssociationsPopupElementView() { Margin = new Thickness(0, 10) };
                view.SetBinding(UtilityAssociationsPopupElementView.ElementProperty, static (PopupElement element) => element);
                return view;
            });
            UnsupportedPopupElementTemplate = new DataTemplate(() =>
            {
                return new Grid() { IsVisible = false };
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupElementTemplateSelector"/> class.
        /// </summary>
        public PopupElementTemplateSelector() 
        {
            TextPopupElementTemplate = DefaultTextPopupElementTemplate;
            MediaPopupElementTemplate = DefaultMediaPopupElementTemplate;
            FieldsPopupElementTemplate = DefaultFieldsPopupElementTemplate;
            AttachmentsPopupElementTemplate = DefaultAttachmentsPopupElementTemplate;
            UtilityAssociationsPopupElementTemplate = DefaultUtilityAssociationsPopupElementTemplate;
        }

        /// <inheritdoc />
        protected override DataTemplate OnSelectTemplate(object item, BindableObject element)
        {
            if (item is TextPopupElement)
            {
                return TextPopupElementTemplate;
            }
            else if (item is MediaPopupElement)
            {
                return MediaPopupElementTemplate;
            }
            else if (item is FieldsPopupElement)
            {
                return FieldsPopupElementTemplate;
            }
            else if (item is AttachmentsPopupElement)
            {
                return AttachmentsPopupElementTemplate;
            }
            else if (item is UtilityAssociationsPopupElement)
            {
                return UtilityAssociationsPopupElementTemplate;
            }
            /* Excluded for now - Pending UI Experience design
            else if(item is RelationshipPopupElement)
            {
                return RelationshipPopupElementTemplate;
            } */
            else if (item is ExpressionPopupElement)
            {
                // This shouldn't happen since the evaluated elements are
                // always returned as TextPopupElement.
                Debug.Assert(false);
            }
            return UnsupportedPopupElementTemplate;
        }

        /// <summary>
        /// Template used for rendering a <see cref="TextPopupElement"/>.
        /// </summary>
        /// <seealso cref="TextPopupElementView"/>
        public DataTemplate TextPopupElementTemplate { get; set; }

        /// <summary>
        /// Template used for rendering a <see cref="MediaPopupElement"/>.
        /// </summary>
        /// <seealso cref="MediaPopupElementView"/>
        public DataTemplate MediaPopupElementTemplate { get; set; }

        /// <summary>
        /// Template used for rendering a <see cref="FieldsPopupElement"/>.
        /// </summary>
        /// <seealso cref="FieldsPopupElementView"/>
        public DataTemplate FieldsPopupElementTemplate { get; set; }

        /// <summary>
        /// Template used for rendering a <see cref="AttachmentsPopupElement"/>.
        /// </summary>
        /// <seealso cref="AttachmentsPopupElementView"/>
        public DataTemplate AttachmentsPopupElementTemplate { get; set; }

        /// <summary>
        /// Template used for rendering a <see cref="UtilityAssociationsPopupElement"/>.
        /// </summary>
        /// <seealso cref="UtilityAssociationsPopupElement"/>
        public DataTemplate UtilityAssociationsPopupElementTemplate { get; set; }

        /* Excluded for now - Pending UI Experience design
        /// <summary>
        /// Template used for rendering a <see cref="RelationshipPopupElement"/>.
        /// </summary>
        /// <seealso cref="RelationshipPopupElementView"/>
        public DataTemplate RelationshipPopupElementTemplate { get; set; }
*/
    }
}
#endif