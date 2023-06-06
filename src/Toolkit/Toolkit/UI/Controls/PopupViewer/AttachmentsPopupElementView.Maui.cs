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
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="AttachmentsPopupElement"/>.
    /// </summary>
    public partial class AttachmentsPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        /// <summary>
        /// Template name of the <see cref="CollectionView"/> attachment list.
        /// </summary>
        public const string AttachmentListName = "AttachmentList";

        static AttachmentsPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            StackLayout root = new StackLayout();
            Label roottitle = new Label();
            roottitle.SetBinding(Label.TextProperty, new Binding("Element.Title", source: RelativeBindingSource.TemplatedParent));
            roottitle.SetBinding(VisualElement.IsVisibleProperty, new Binding("Element.Title", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            root.Add(roottitle);
            Label rootcaption = new Label();
            rootcaption.SetBinding(Label.TextProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent));
            rootcaption.SetBinding(VisualElement.IsVisibleProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            root.Add(rootcaption);
            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.Single };
            cv.SetBinding(CollectionView.ItemsSourceProperty, new Binding("Element.Attachments", source: RelativeBindingSource.TemplatedParent));
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            root.Add(cv);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(AttachmentListName, cv);
            return root;
        }

        private static object BuildDefaultItemTemplate()
        {
            Label attachment = new Label();
            attachment.SetBinding(Label.TextProperty, "Name");
            return attachment;
        }

        /// <summary>
        /// Occurs when an attachment is clicked.
        /// </summary>
        /// <remarks>Override this to prevent the default "save to file dialog" action.</remarks>
        /// <param name="attachment">Attachment clicked.</param>
        public virtual void OnAttachmentClicked(PopupAttachment attachment)
        {
        }
    }
}
#endif