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
using Microsoft.Win32;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="AttachmentsPopupElement"/>.
    /// </summary>
    public partial class AttachmentsPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static AttachmentsPopupElementView()
        {
            string template = """
<ControlTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui" xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" 
    xmlns:esriP="clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui.Primitives"
    x:DataType="esriP:AttachmentsPopupElementView" x:Name="Self">
     <StackLayout>
         <Label Text="{TemplateBinding Title}" />
         <Label Text="{TemplateBinding Description}" />
         <CollectionView x:Name="AttachmentList">
             <CollectionView.ItemTemplate>
                  <DataTemplate>
                        <Label Text="{Binding Name}" Margin="5" />
                  </DataTemplate>
             </CollectionView.ItemTemplate>
         </CollectionView>
     </StackLayout>
</ControlTemplate>
""";
            DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);
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